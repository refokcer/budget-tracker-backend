namespace budget_tracker_backend.Services.Algorithms.BudgetPlanning;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AutoBudgetPlanAlgorithm : IAutoBudgetPlanAlgorithm
{
    private const decimal MinGeneratedAmount = 0m;
    private const decimal MaxAutomaticMultiplier = 2m;
    private const int IntelligentAnalysisMonths = 6;

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AutoBudgetPlanAlgorithm(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public string Code => "auto-budget-plan";
    public string Name => "Automatic Budget Plan";

    public async Task<AutoBudgetPlanResultDto> CreateMonthlyPlanAsync(
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
        var analysisStart = targetStart.AddMonths(-IntelligentAnalysisMonths);
        var analysisTransactions = await _context.Transactions
            .Where(t => t.Date >= analysisStart && t.Date < previousEndExclusive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var expenseHistoryByCategory = analysisTransactions
            .Where(t => t.Type == TransactionCategoryType.Expense && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var activeMonths = Math.Max(1, g.Select(t => new { t.Date.Year, t.Date.Month }).Distinct().Count());
                    return g.Sum(t => t.Amount) / activeMonths;
                });

        var averageMonthlyIncome = CalculateAverageMonthlyIncome(analysisTransactions);
        var goalReserve = await CalculateMonthlyGoalReserveAsync(targetStart, cancellationToken);
        var previousFinancialStateIndex = CalculateFinancialStateIndex(
            analysisTransactions,
            analysisStart,
            analysisStart.AddMonths(3));
        var financialStateIndex = CalculateFinancialStateIndex(
            analysisTransactions,
            analysisStart.AddMonths(3),
            previousEndExclusive);
        var financialStateTrend = financialStateIndex - previousFinancialStateIndex;

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

        var generatedItems = new List<AutoGeneratedBudgetItem>();

        foreach (var sourceItem in sourceItems)
        {
            spentByCategory.TryGetValue(sourceItem.CategoryId, out var spentAmount);
            expenseHistoryByCategory.TryGetValue(sourceItem.CategoryId, out var historyAverageAmount);

            var previousLimit = sourceItem.Amount;
            var remainingAmount = Math.Max(0m, previousLimit - spentAmount);
            var overspentAmount = Math.Max(0m, spentAmount - previousLimit);
            var carryAdjustment = overspentAmount * overspendCarryRate
                - remainingAmount * underspendCarryRate;
            var adjustedBase = Math.Max(MinGeneratedAmount, previousLimit + carryAdjustment);
            var priority = ResolveCategoryPriority(sourceItem);
            var historyWeightedBase = BlendWithHistory(adjustedBase, historyAverageAmount, priority);
            var sourceSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(previousStart.Month, sourceItem, new Dictionary<int, decimal>())
                : 1m;
            var targetSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(targetStart.Month, sourceItem, overrides)
                : 1m;
            var seasonalityMultiplier = sourceSeasonalityMultiplier > 0m
                ? ClampMultiplier(targetSeasonalityMultiplier / sourceSeasonalityMultiplier)
                : targetSeasonalityMultiplier;
            var financialStateMultiplier = ResolveFinancialStateMultiplier(priority, financialStateTrend);
            var recommendedBeforeEnvelope = Math.Round(historyWeightedBase * seasonalityMultiplier * financialStateMultiplier, 2);

            generatedItems.Add(new AutoGeneratedBudgetItem
            {
                CategoryId = sourceItem.CategoryId,
                CategoryTitle = sourceItem.Category?.Title ?? $"Category #{sourceItem.CategoryId}",
                PreviousLimit = Math.Round(previousLimit, 2),
                SpentAmount = Math.Round(spentAmount, 2),
                RemainingAmount = Math.Round(remainingAmount, 2),
                OverspentAmount = Math.Round(overspentAmount, 2),
                CarryAdjustment = Math.Round(carryAdjustment, 2),
                HistoryAverageAmount = Math.Round(historyAverageAmount, 2),
                Priority = priority,
                SeasonalityMultiplier = seasonalityMultiplier,
                FinancialStateMultiplier = financialStateMultiplier,
                RecommendedAmount = Math.Max(MinGeneratedAmount, recommendedBeforeEnvelope),
                CurrencyId = sourceItem.CurrencyId,
                Description = BuildAutoItemDescription(
                    sourceItem,
                    previousStart.Month,
                    targetStart.Month,
                    sourceSeasonalityMultiplier,
                    targetSeasonalityMultiplier)
            });
        }

        var expenseEnvelope = CalculateExpenseEnvelope(averageMonthlyIncome, goalReserve, financialStateIndex);
        var incomeEnvelopeMultiplier = ApplyIncomeEnvelope(generatedItems, expenseEnvelope);

        var itemResults = new List<AutoBudgetPlanItemResultDto>();
        foreach (var generatedItem in generatedItems)
        {
            var newItem = new BudgetPlanItem
            {
                CategoryId = generatedItem.CategoryId,
                Amount = generatedItem.RecommendedAmount,
                CurrencyId = generatedItem.CurrencyId,
                Description = BuildIntelligentAutoItemDescription(generatedItem, financialStateTrend, goalReserve)
            };

            plan.Items.Add(newItem);
            itemResults.Add(new AutoBudgetPlanItemResultDto
            {
                CategoryId = generatedItem.CategoryId,
                CategoryTitle = generatedItem.CategoryTitle,
                PreviousLimit = generatedItem.PreviousLimit,
                SpentAmount = generatedItem.SpentAmount,
                RemainingAmount = generatedItem.RemainingAmount,
                OverspentAmount = generatedItem.OverspentAmount,
                CarryAdjustment = generatedItem.CarryAdjustment,
                HistoryAverageAmount = generatedItem.HistoryAverageAmount,
                Priority = generatedItem.Priority.ToString(),
                SeasonalityMultiplier = generatedItem.SeasonalityMultiplier,
                FinancialStateMultiplier = generatedItem.FinancialStateMultiplier,
                IncomeEnvelopeMultiplier = generatedItem.IncomeEnvelopeMultiplier,
                RecommendedAmount = generatedItem.RecommendedAmount,
                CurrencyId = generatedItem.CurrencyId,
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
            AverageMonthlyIncome = Math.Round(averageMonthlyIncome, 2),
            GoalReserve = Math.Round(goalReserve, 2),
            ExpenseEnvelope = Math.Round(expenseEnvelope, 2),
            IncomeEnvelopeMultiplier = Math.Round(incomeEnvelopeMultiplier, 4),
            FinancialStateIndex = Math.Round(financialStateIndex, 4),
            PreviousFinancialStateIndex = Math.Round(previousFinancialStateIndex, 4),
            FinancialStateTrend = Math.Round(financialStateTrend, 4),
            Items = itemResults
        };
    }

    private static DateTime ResolveTargetMonth(int? month, int? year)
    {
        var defaultTarget = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(1);
        var targetMonth = month ?? defaultTarget.Month;
        var targetYear = year ?? defaultTarget.Year;

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

        if (month is 6 or 7 or 8 && IsTravelCategory(categoryText))
            multiplier = Math.Max(multiplier, 1.2m);

        if (month == 9 && IsSchoolCategory(categoryText))
            multiplier = Math.Max(multiplier, 1.25m);

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
            "подар", "праздн", "свят");
    }

    private static bool IsFoodOrEntertainmentCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "food", "grocery", "groceries", "restaurant", "entertainment",
            "продукт", "еда", "ресторан", "кафе");
    }

    private static bool IsUtilityCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "utility", "utilities", "heating", "heat", "gas", "electric", "electricity", "water",
            "коммун", "комун", "отоп", "опален", "электр", "електр", "вода");
    }

    private static bool IsTransportCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "transport", "fuel", "taxi", "car",
            "транспорт", "бензин", "авто", "такси");
    }

    private static bool IsTravelCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "travel", "vacation", "trip", "hotel", "tickets",
            "отпуск", "відпуст", "путеш", "подорож", "отел", "готел", "билет", "квитк");
    }

    private static bool IsSchoolCategory(string categoryText)
    {
        return ContainsAny(categoryText,
            "school", "education", "kids", "children", "books",
            "школ", "учеб", "навч", "дети", "діти", "книг");
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

    private static decimal CalculateAverageMonthlyIncome(List<Transaction> transactions)
    {
        var incomeMonths = transactions
            .Where(t => t.Type == TransactionCategoryType.Income)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(t => t.Amount))
            .Where(amount => amount > 0m)
            .ToList();

        return incomeMonths.Count == 0
            ? 0m
            : incomeMonths.Average();
    }

    private async Task<decimal> CalculateMonthlyGoalReserveAsync(
        DateTime targetMonthStart,
        CancellationToken cancellationToken)
    {
        var goals = await _context.FinancialGoals
            .AsNoTracking()
            .Where(g => g.TargetDate >= targetMonthStart && g.TargetAmount > g.InitialAmount)
            .ToListAsync(cancellationToken);

        return goals.Sum(goal =>
        {
            var monthsRemaining = Math.Max(
                1,
                ((goal.TargetDate.Year - targetMonthStart.Year) * 12)
                + goal.TargetDate.Month
                - targetMonthStart.Month
                + 1);
            return Math.Max(0m, goal.TargetAmount - goal.InitialAmount) / monthsRemaining;
        });
    }

    private static decimal CalculateFinancialStateIndex(
        List<Transaction> transactions,
        DateTime start,
        DateTime end)
    {
        var windowTransactions = transactions
            .Where(t => t.Date >= start && t.Date < end)
            .ToList();
        var income = windowTransactions
            .Where(t => t.Type == TransactionCategoryType.Income)
            .Sum(t => t.Amount);
        var expenses = windowTransactions
            .Where(t => t.Type == TransactionCategoryType.Expense)
            .Sum(t => t.Amount);

        if (income <= 0m)
            return 0.65m;

        var savingsRatio = ClampRate((income - expenses) / income);
        var expenseLoadScore = 1m - ClampRate(expenses / income);

        return ClampRate(savingsRatio * 0.65m + expenseLoadScore * 0.35m);
    }

    private static AutoCategoryPriority ResolveCategoryPriority(BudgetPlanItem item)
    {
        var categoryText = BuildCategoryText(item);

        if (ContainsAny(categoryText,
            "rent", "mortgage", "loan", "debt", "insurance", "tax", "medical", "medicine",
            "аренд", "оренд", "ипотек", "іпотек", "кредит", "долг", "борг", "мед"))
            return AutoCategoryPriority.Essential;

        if (IsUtilityCategory(categoryText)
            || IsTransportCategory(categoryText)
            || ContainsAny(categoryText, "grocery", "groceries", "продукт", "супермаркет"))
            return AutoCategoryPriority.Essential;

        if (ContainsAny(categoryText,
            "entertainment", "delivery", "restaurant", "fastfood", "fast food", "shopping", "gift", "coffee", "subscription",
            "развл", "достав", "ресторан", "фаст", "покуп", "подар", "кофе"))
            return AutoCategoryPriority.Discretionary;

        return AutoCategoryPriority.Flexible;
    }

    private static decimal BlendWithHistory(
        decimal adjustedBase,
        decimal historyAverageAmount,
        AutoCategoryPriority priority)
    {
        if (historyAverageAmount <= 0m)
            return adjustedBase;

        var historyWeight = priority switch
        {
            AutoCategoryPriority.Essential => 0.25m,
            AutoCategoryPriority.Flexible => 0.40m,
            AutoCategoryPriority.Discretionary => 0.55m,
            _ => 0.40m
        };

        return Math.Max(MinGeneratedAmount, adjustedBase * (1m - historyWeight) + historyAverageAmount * historyWeight);
    }

    private static decimal ResolveFinancialStateMultiplier(
        AutoCategoryPriority priority,
        decimal financialStateTrend)
    {
        if (financialStateTrend >= -0.03m)
            return 1m;

        var deterioration = ClampRate(Math.Abs(financialStateTrend) / 0.35m);
        var reduction = priority switch
        {
            AutoCategoryPriority.Essential => deterioration * 0.06m,
            AutoCategoryPriority.Flexible => deterioration * 0.16m,
            AutoCategoryPriority.Discretionary => deterioration * 0.32m,
            _ => deterioration * 0.16m
        };

        return Math.Round(Math.Max(0.68m, 1m - reduction), 4);
    }

    private static decimal CalculateExpenseEnvelope(
        decimal averageMonthlyIncome,
        decimal goalReserve,
        decimal financialStateIndex)
    {
        if (averageMonthlyIncome <= 0m)
            return 0m;

        var safetyReserveRate = financialStateIndex < 0.45m
            ? 0.12m
            : financialStateIndex < 0.65m
                ? 0.08m
                : 0.05m;
        var safetyReserve = averageMonthlyIncome * safetyReserveRate;

        return Math.Max(0m, averageMonthlyIncome - goalReserve - safetyReserve);
    }

    private static decimal ApplyIncomeEnvelope(
        List<AutoGeneratedBudgetItem> items,
        decimal expenseEnvelope)
    {
        var total = items.Sum(i => i.RecommendedAmount);
        if (expenseEnvelope <= 0m || total <= expenseEnvelope)
            return 1m;

        var remainingReduction = total - expenseEnvelope;
        foreach (var item in items.OrderByDescending(i => i.Priority))
        {
            if (remainingReduction <= 0m)
                break;

            var maxCutRate = item.Priority switch
            {
                AutoCategoryPriority.Discretionary => 0.35m,
                AutoCategoryPriority.Flexible => 0.20m,
                AutoCategoryPriority.Essential => 0.08m,
                _ => 0.20m
            };
            var maxCutAmount = Math.Round(item.RecommendedAmount * maxCutRate, 2);
            var cutAmount = Math.Min(maxCutAmount, remainingReduction);

            item.RecommendedAmount = Math.Round(Math.Max(MinGeneratedAmount, item.RecommendedAmount - cutAmount), 2);
            item.IncomeEnvelopeMultiplier = item.OriginalRecommendedAmount > 0m
                ? Math.Round(item.RecommendedAmount / item.OriginalRecommendedAmount, 4)
                : 1m;
            remainingReduction -= cutAmount;
        }

        return total > 0m
            ? Math.Round(items.Sum(i => i.RecommendedAmount) / total, 4)
            : 1m;
    }

    private static string? BuildIntelligentAutoItemDescription(
        AutoGeneratedBudgetItem item,
        decimal financialStateTrend,
        decimal goalReserve)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.Description))
            parts.Add(item.Description);

        if (financialStateTrend < -0.03m && item.FinancialStateMultiplier < 1m)
        {
            parts.Add(item.Priority == AutoCategoryPriority.Discretionary
                ? "Reduced because financial stability declined and category is discretionary."
                : "Reduced because financial stability declined.");
        }

        if (goalReserve > 0m && item.IncomeEnvelopeMultiplier < 1m)
            parts.Add("Adjusted to reserve money for financial goals.");

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private enum AutoCategoryPriority
    {
        Essential = 1,
        Flexible = 2,
        Discretionary = 3
    }

    private sealed class AutoGeneratedBudgetItem
    {
        public int CategoryId { get; set; }
        public string CategoryTitle { get; set; } = null!;
        public decimal PreviousLimit { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal OverspentAmount { get; set; }
        public decimal CarryAdjustment { get; set; }
        public decimal HistoryAverageAmount { get; set; }
        public AutoCategoryPriority Priority { get; set; }
        public decimal SeasonalityMultiplier { get; set; }
        public decimal FinancialStateMultiplier { get; set; } = 1m;
        public decimal IncomeEnvelopeMultiplier { get; set; } = 1m;
        public decimal RecommendedAmount
        {
            get => _recommendedAmount;
            set
            {
                _recommendedAmount = value;
                if (OriginalRecommendedAmount == 0m)
                    OriginalRecommendedAmount = value;
            }
        }
        public decimal OriginalRecommendedAmount { get; private set; }
        public int CurrencyId { get; set; }
        public string? Description { get; set; }

        private decimal _recommendedAmount;
    }
}
