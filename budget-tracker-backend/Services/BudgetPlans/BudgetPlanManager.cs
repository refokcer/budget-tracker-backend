namespace budget_tracker_backend.Services.BudgetPlans;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class BudgetPlanManager : IBudgetPlanManager
{
    private const decimal MinGeneratedAmount = 0m;
    private const decimal MaxAutomaticMultiplier = 2m;

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BudgetPlanManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BudgetPlan>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.BudgetPlans
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetPlan?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.BudgetPlans.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<BudgetPlan> CreateAsync(CreateBudgetPlanDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<BudgetPlan>(dto) ??
            throw new CustomException("Cannot map CreateBudgetPlanDto", StatusCodes.Status400BadRequest);

        entity.ParentId = await ValidateParentAsync(entity.Type, dto.ParentId, cancellationToken);

        await _context.BudgetPlans.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create budget plan", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<AutoBudgetPlanResultDto> CreateAutoMonthlyPlanAsync(
        AutoBudgetPlanRequestDto dto,
        CancellationToken cancellationToken)
    {
        var targetStart = ResolveTargetMonth(dto.Month, dto.Year);
        var targetEndExclusive = targetStart.AddMonths(1);
        var targetEnd = targetEndExclusive.AddDays(-1);
        var previousStart = targetStart.AddMonths(-1);
        var previousEndExclusive = targetStart;

        var sourcePlan = await _context.BudgetPlans
            .Include(p => p.Items!)
                .ThenInclude(i => i.Category)
            .AsNoTracking()
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < previousEndExclusive
                && p.EndDate >= previousStart)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourcePlan == null)
            throw new CustomException("Previous monthly budget plan was not found", StatusCodes.Status400BadRequest);

        var existingPlans = await _context.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < targetEndExclusive
                && p.EndDate >= targetStart)
            .ToListAsync(cancellationToken);

        if (existingPlans.Count > 0 && !dto.ReplaceExisting)
            throw new CustomException("Monthly budget plan already exists for the target month", StatusCodes.Status400BadRequest);

        if (existingPlans.Count > 0)
            _context.BudgetPlans.RemoveRange(existingPlans);

        var sourceItems = (sourcePlan.Items ?? [])
            .Where(i => i.Amount >= 0m)
            .ToList();

        if (sourceItems.Count == 0)
            throw new CustomException("Previous monthly budget plan has no items to copy", StatusCodes.Status400BadRequest);

        var sourceCategoryIds = sourceItems
            .Select(i => i.CategoryId)
            .Distinct()
            .ToList();

        var spentByCategory = await _context.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.BudgetPlanId == sourcePlan.Id
                && t.CategoryId != null
                && sourceCategoryIds.Contains(t.CategoryId.Value)
                && t.Date >= previousStart
                && t.Date < previousEndExclusive)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, SpentAmount = g.Sum(t => t.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.SpentAmount, cancellationToken);

        var overspendCarryRate = ClampRate(dto.OverspendCarryRate);
        var underspendCarryRate = ClampRate(dto.UnderspendCarryRate);
        var overrides = dto.SeasonalityOverrides
            .GroupBy(o => o.CategoryId)
            .ToDictionary(g => g.Key, g => ClampMultiplier(g.Last().Multiplier));

        var plan = new BudgetPlan
        {
            Title = string.IsNullOrWhiteSpace(dto.Title)
                ? $"Auto plan {targetStart:MMMM yyyy}"
                : dto.Title.Trim(),
            StartDate = targetStart,
            EndDate = targetEnd,
            Type = BudgetPlanType.Monthly,
            Description = $"Generated from \"{sourcePlan.Title}\" with overspend, remaining budget, and seasonal coefficients.",
            ParentId = null,
            Items = new List<BudgetPlanItem>()
        };

        var itemResults = new List<AutoBudgetPlanItemResultDto>();

        foreach (var sourceItem in sourceItems)
        {
            spentByCategory.TryGetValue(sourceItem.CategoryId, out var spentAmount);

            var previousLimit = sourceItem.Amount;
            var remainingAmount = Math.Max(0m, previousLimit - spentAmount);
            var overspentAmount = Math.Max(0m, spentAmount - previousLimit);
            var carryAdjustment = overspentAmount * overspendCarryRate
                - remainingAmount * underspendCarryRate;
            var adjustedBase = Math.Max(MinGeneratedAmount, previousLimit + carryAdjustment);
            var sourceSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(previousStart.Month, sourceItem, new Dictionary<int, decimal>())
                : 1m;
            var targetSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(targetStart.Month, sourceItem, overrides)
                : 1m;
            var seasonalityMultiplier = sourceSeasonalityMultiplier > 0m
                ? ClampMultiplier(targetSeasonalityMultiplier / sourceSeasonalityMultiplier)
                : targetSeasonalityMultiplier;
            var recommendedAmount = Math.Round(adjustedBase * seasonalityMultiplier, 2);

            var newItem = new BudgetPlanItem
            {
                CategoryId = sourceItem.CategoryId,
                Amount = recommendedAmount,
                CurrencyId = sourceItem.CurrencyId,
                Description = BuildAutoItemDescription(
                    sourceItem,
                    previousStart.Month,
                    targetStart.Month,
                    sourceSeasonalityMultiplier,
                    targetSeasonalityMultiplier)
            };

            plan.Items.Add(newItem);
            itemResults.Add(new AutoBudgetPlanItemResultDto
            {
                CategoryId = sourceItem.CategoryId,
                CategoryTitle = sourceItem.Category?.Title ?? $"Category #{sourceItem.CategoryId}",
                PreviousLimit = Math.Round(previousLimit, 2),
                SpentAmount = Math.Round(spentAmount, 2),
                RemainingAmount = Math.Round(remainingAmount, 2),
                OverspentAmount = Math.Round(overspentAmount, 2),
                CarryAdjustment = Math.Round(carryAdjustment, 2),
                SeasonalityMultiplier = seasonalityMultiplier,
                RecommendedAmount = recommendedAmount,
                CurrencyId = sourceItem.CurrencyId,
                Description = newItem.Description
            });
        }

        await _context.BudgetPlans.AddAsync(plan, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create automatic monthly budget plan", StatusCodes.Status500InternalServerError);

        var createdItems = plan.Items?.ToList() ?? [];
        for (var i = 0; i < itemResults.Count && i < createdItems.Count; i++)
            itemResults[i].Id = createdItems[i].Id;

        return new AutoBudgetPlanResultDto
        {
            Plan = _mapper.Map<BudgetPlanDto>(plan),
            SourcePlanId = sourcePlan.Id,
            SourcePlanTitle = sourcePlan.Title,
            SourceStartDate = sourcePlan.StartDate,
            SourceEndDate = sourcePlan.EndDate,
            PreviousTotal = Math.Round(sourceItems.Sum(i => i.Amount), 2),
            NewTotal = Math.Round(itemResults.Sum(i => i.RecommendedAmount), 2),
            Items = itemResults
        };
    }

    public async Task<BudgetPlan> UpdateAsync(BudgetPlanDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.BudgetPlans.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Budget plan not found", StatusCodes.Status404NotFound);

        existing.Title = dto.Title;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.Type = dto.Type;
        existing.Description = dto.Description;
        existing.ParentId = await ValidateParentAsync(dto.Type, dto.ParentId, cancellationToken);

        _context.BudgetPlans.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update budget plan", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var plan = await _context.BudgetPlans.FindAsync(new object[] { id }, cancellationToken);
        if (plan == null)
            throw new CustomException("Budget plan not found", StatusCodes.Status404NotFound);

        _context.BudgetPlans.Remove(plan);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete budget plan", StatusCodes.Status500InternalServerError);

        return true;
    }

    private async Task<int?> ValidateParentAsync(BudgetPlanType type, int? parentId, CancellationToken cancellationToken)
    {
        if (type == BudgetPlanType.Monthly)
        {
            if (parentId.HasValue)
                throw new CustomException("Monthly plan cannot have parent", StatusCodes.Status400BadRequest);
            return null;
        }

        if (!parentId.HasValue)
            return null;

        var parent = await _context.BudgetPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == parentId.Value, cancellationToken);
        if (parent == null || parent.Type != BudgetPlanType.Monthly)
            throw new CustomException("Invalid parent plan", StatusCodes.Status400BadRequest);

        return parentId;
    }

    private static DateTime ResolveTargetMonth(int? month, int? year)
    {
        var now = DateTime.UtcNow;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        if (targetMonth is < 1 or > 12)
            throw new CustomException("Month must be between 1 and 12", StatusCodes.Status400BadRequest);

        if (targetYear is < 2000 or > 2100)
            throw new CustomException("Year must be between 2000 and 2100", StatusCodes.Status400BadRequest);

        return new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static decimal ResolveSeasonalityMultiplier(
        int month,
        BudgetPlanItem item,
        Dictionary<int, decimal> overrides)
    {
        if (overrides.TryGetValue(item.CategoryId, out var overrideMultiplier))
            return overrideMultiplier;

        var categoryText = BuildCategoryText(item);
        var multiplier = 1m;

        if (month == 12)
        {
            multiplier = Math.Max(multiplier, 1.05m);

            if (IsHolidayCategory(categoryText))
                multiplier = Math.Max(multiplier, 1.4m);

            if (IsFoodOrEntertainmentCategory(categoryText))
                multiplier = Math.Max(multiplier, 1.15m);
        }

        if (month is 12 or 1 or 2)
        {
            if (IsUtilityCategory(categoryText))
                multiplier = Math.Max(multiplier, 1.25m);

            if (IsTransportCategory(categoryText))
                multiplier = Math.Max(multiplier, 1.1m);
        }

        if (month is 6 or 7 or 8
            && IsTravelCategory(categoryText))
        {
            multiplier = Math.Max(multiplier, 1.2m);
        }

        if (month == 9
            && IsSchoolCategory(categoryText))
        {
            multiplier = Math.Max(multiplier, 1.25m);
        }

        return ClampMultiplier(multiplier);
    }

    private static string BuildCategoryText(BudgetPlanItem item)
    {
        return $"{item.Category?.Title} {item.Category?.Description} {item.Description}".ToLowerInvariant();
    }

    private static bool IsHolidayCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "gift", "present", "holiday", "celebration", "party",
            "\u043f\u043e\u0434\u0430\u0440", "\u043f\u0440\u0430\u0437\u0434\u043d", "\u0441\u0432\u044f\u0442");
    }

    private static bool IsFoodOrEntertainmentCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "food", "grocery", "groceries", "restaurant", "entertainment",
            "\u043f\u0440\u043e\u0434\u0443\u043a\u0442", "\u0435\u0434\u0430", "\u0440\u0435\u0441\u0442\u043e\u0440\u0430\u043d", "\u043a\u0430\u0444\u0435");
    }

    private static bool IsUtilityCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "utility", "utilities", "heating", "heat", "gas", "electric", "electricity", "water",
            "\u043a\u043e\u043c\u043c\u0443\u043d", "\u043a\u043e\u043c\u0443\u043d", "\u043e\u0442\u043e\u043f", "\u043e\u043f\u0430\u043b\u0435\u043d", "\u044d\u043b\u0435\u043a\u0442\u0440", "\u0435\u043b\u0435\u043a\u0442\u0440", "\u0432\u043e\u0434\u0430");
    }

    private static bool IsTransportCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "transport", "fuel", "taxi", "car",
            "\u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442", "\u0431\u0435\u043d\u0437\u0438\u043d", "\u0430\u0432\u0442\u043e", "\u0442\u0430\u043a\u0441\u0438");
    }

    private static bool IsTravelCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "travel", "vacation", "trip", "hotel", "tickets",
            "\u043e\u0442\u043f\u0443\u0441\u043a", "\u0432\u0456\u0434\u043f\u0443\u0441\u0442", "\u043f\u0443\u0442\u0435\u0448", "\u043f\u043e\u0434\u043e\u0440\u043e\u0436", "\u043e\u0442\u0435\u043b", "\u0433\u043e\u0442\u0435\u043b", "\u0431\u0438\u043b\u0435\u0442", "\u043a\u0432\u0438\u0442\u043a");
    }

    private static bool IsSchoolCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "school", "education", "kids", "children", "books",
            "\u0448\u043a\u043e\u043b", "\u0443\u0447\u0435\u0431", "\u043d\u0430\u0432\u0447", "\u0434\u0435\u0442\u0438", "\u0434\u0456\u0442\u0438", "\u043a\u043d\u0438\u0433");
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(value.Contains);
    }

    private static decimal ClampRate(decimal value)
    {
        return Math.Min(1m, Math.Max(0m, value));
    }

    private static decimal ClampMultiplier(decimal value)
    {
        return Math.Min(MaxAutomaticMultiplier, Math.Max(0m, value));
    }

    private static string? BuildAutoItemDescription(
        BudgetPlanItem item,
        int sourceMonth,
        int targetMonth,
        decimal sourceSeasonalityMultiplier,
        decimal targetSeasonalityMultiplier)
    {
        if (targetSeasonalityMultiplier == sourceSeasonalityMultiplier)
            return null;

        var categoryText = BuildCategoryText(item);
        var isRaised = targetSeasonalityMultiplier > sourceSeasonalityMultiplier;

        if (isRaised && targetMonth == 12 && IsHolidayCategory(categoryText))
            return "Higher because of holidays.";

        if (!isRaised && sourceMonth == 12 && IsHolidayCategory(categoryText))
            return "Lower because holiday season ended.";

        if (isRaised && targetMonth is 12 or 1 or 2 && IsUtilityCategory(categoryText))
            return "Higher because of winter utilities.";

        if (!isRaised && sourceMonth is 12 or 1 or 2 && IsUtilityCategory(categoryText))
            return "Lower because winter season ended.";

        if (isRaised && targetMonth is 6 or 7 or 8 && IsTravelCategory(categoryText))
            return "Higher because of summer travel season.";

        if (!isRaised && sourceMonth is 6 or 7 or 8 && IsTravelCategory(categoryText))
            return "Lower because travel season ended.";

        if (isRaised && targetMonth == 9 && IsSchoolCategory(categoryText))
            return "Higher because of school season.";

        if (!isRaised && sourceMonth == 9 && IsSchoolCategory(categoryText))
            return "Lower because school season ended.";

        return null;
    }

}
