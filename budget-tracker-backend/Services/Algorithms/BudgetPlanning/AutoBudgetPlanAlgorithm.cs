namespace budget_tracker_backend.Services.Algorithms.BudgetPlanning;

using System.Text.Json;
using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Dto.UserSettings;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.RecurringPayments;
using budget_tracker_backend.Services.UserSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AutoBudgetPlanAlgorithm : IAutoBudgetPlanAlgorithm
{
    private const decimal MinGeneratedAmount = 0m;
    private const decimal MaxAutomaticMultiplier = 2m;
    private const int IntelligentAnalysisMonths = 6;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var rules = await LoadAutoBudgetPlanRulesAsync(sourcePlan.UserId, cancellationToken);

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

        var recurringPayments = await _context.RecurringPayments
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.StartDate < targetEndExclusive
                && (p.EndDate == null || p.EndDate >= targetStart))
            .ToListAsync(cancellationToken);

        var recurringOccurrences = recurringPayments
            .SelectMany(payment => RecurringPaymentSchedule.GetOccurrences(payment, targetStart, targetEndExclusive)
                .Select(date => new { Payment = payment, Date = date }))
            .ToList();

        var recurringExpenseByCategory = recurringOccurrences
            .Where(o => o.Payment.Type == TransactionCategoryType.Expense && o.Payment.CategoryId.HasValue)
            .GroupBy(o => o.Payment.CategoryId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new RecurringBudgetCommitment
                {
                    CategoryId = g.Key,
                    CategoryTitle = g.First().Payment.Category?.Title ?? $"Category #{g.Key}",
                    Category = g.First().Payment.Category,
                    CurrencyId = g.First().Payment.CurrencyId,
                    Amount = g.Sum(o => o.Payment.Amount)
                });

        var recurringMonthlyIncome = recurringOccurrences
            .Where(o => o.Payment.Type == TransactionCategoryType.Income)
            .Sum(o => o.Payment.Amount);

        var averageMonthlyIncome = Math.Max(
            CalculateAverageMonthlyIncome(analysisTransactions),
            recurringMonthlyIncome);
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
            Description = $"Generated from \"{sourcePlan.Title}\" with overspend, remaining budget, recurring payments, and configured category rules.",
            ParentId = null,
            Items = new List<BudgetPlanItem>()
        };

        var generatedItems = new List<AutoGeneratedBudgetItem>();

        foreach (var sourceItem in sourceItems)
        {
            spentByCategory.TryGetValue(sourceItem.CategoryId, out var spentAmount);
            expenseHistoryByCategory.TryGetValue(sourceItem.CategoryId, out var historyAverageAmount);
            recurringExpenseByCategory.TryGetValue(sourceItem.CategoryId, out var recurringCommitment);

            var previousLimit = sourceItem.Amount;
            var remainingAmount = Math.Max(0m, previousLimit - spentAmount);
            var overspentAmount = Math.Max(0m, spentAmount - previousLimit);
            var categoryRule = rules.GetCategoryRule(sourceItem.CategoryId);
            var isProtected = categoryRule.CutBehavior == AutoBudgetPlanCutBehavior.Protected;
            var isAggressiveCut = categoryRule.CutBehavior == AutoBudgetPlanCutBehavior.Aggressive;
            var carryAdjustment = overspentAmount * overspendCarryRate
                - (isProtected ? 0m : remainingAmount * underspendCarryRate);
            var adjustedBase = Math.Max(MinGeneratedAmount, previousLimit + carryAdjustment);
            var priority = ResolveCategoryPriority(sourceItem);
            var historyWeightedBase = BlendWithHistory(adjustedBase, historyAverageAmount, priority);
            var sourceSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(previousStart.Month, sourceItem, new Dictionary<int, decimal>(), categoryRule)
                : 1m;
            var targetSeasonalityMultiplier = dto.ApplySeasonality
                ? ResolveSeasonalityMultiplier(targetStart.Month, sourceItem, overrides, categoryRule)
                : 1m;
            var seasonalityMultiplier = sourceSeasonalityMultiplier > 0m
                ? ClampMultiplier(targetSeasonalityMultiplier / sourceSeasonalityMultiplier)
                : targetSeasonalityMultiplier;
            var financialStateMultiplier = ResolveFinancialStateMultiplier(priority, financialStateTrend, isProtected, isAggressiveCut);
            var recommendedBeforeEnvelope = Math.Round(historyWeightedBase * seasonalityMultiplier * financialStateMultiplier, 2);
            if (recurringCommitment != null)
                recommendedBeforeEnvelope = Math.Max(recommendedBeforeEnvelope, recurringCommitment.Amount);
            var recommendedAmount = Math.Max(MinGeneratedAmount, recommendedBeforeEnvelope);
            if (isProtected)
                recommendedAmount = Math.Max(recommendedAmount, previousLimit);
            recommendedAmount = ApplyLimitRules(recommendedAmount, recurringCommitment?.Amount ?? 0m, categoryRule);

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
                ProtectedFromCuts = isProtected,
                AggressiveCut = isAggressiveCut,
                SeasonalityMultiplier = seasonalityMultiplier,
                FinancialStateMultiplier = financialStateMultiplier,
                RecommendedAmount = recommendedAmount,
                RecurringCommittedAmount = recurringCommitment?.Amount ?? 0m,
                CurrencyId = sourceItem.CurrencyId,
                Description = BuildAutoItemDescription(
                    sourceItem,
                    previousStart.Month,
                    targetStart.Month,
                    sourceSeasonalityMultiplier,
                    targetSeasonalityMultiplier)
            });
        }

        foreach (var commitment in recurringExpenseByCategory.Values
            .Where(c => !sourceCategoryIds.Contains(c.CategoryId)))
        {
            var syntheticSourceItem = new BudgetPlanItem
            {
                CategoryId = commitment.CategoryId,
                Category = commitment.Category,
                CurrencyId = commitment.CurrencyId,
                Amount = 0m
            };
            var priority = ResolveCategoryPriority(syntheticSourceItem);
            var categoryRule = rules.GetCategoryRule(commitment.CategoryId);
            var recommendedAmount = ApplyLimitRules(
                Math.Round(commitment.Amount, 2),
                commitment.Amount,
                categoryRule);

            generatedItems.Add(new AutoGeneratedBudgetItem
            {
                CategoryId = commitment.CategoryId,
                CategoryTitle = commitment.CategoryTitle,
                PreviousLimit = 0m,
                SpentAmount = 0m,
                RemainingAmount = 0m,
                OverspentAmount = 0m,
                CarryAdjustment = 0m,
                HistoryAverageAmount = expenseHistoryByCategory.GetValueOrDefault(commitment.CategoryId),
                Priority = priority,
                ProtectedFromCuts = categoryRule.CutBehavior == AutoBudgetPlanCutBehavior.Protected,
                AggressiveCut = categoryRule.CutBehavior == AutoBudgetPlanCutBehavior.Aggressive,
                SeasonalityMultiplier = 1m,
                FinancialStateMultiplier = 1m,
                RecommendedAmount = recommendedAmount,
                RecurringCommittedAmount = Math.Round(commitment.Amount, 2),
                CurrencyId = commitment.CurrencyId,
                Description = "Added because recurring payments are scheduled for this category."
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

    private async Task<AutoBudgetPlanRules> LoadAutoBudgetPlanRulesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var claimValue = await _context.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.ClaimType == UserSettingsClaimTypes.AutoBudgetPlanRules)
            .Select(c => c.ClaimValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(claimValue))
            return AutoBudgetPlanRules.Default;

        try
        {
            var dto = JsonSerializer.Deserialize<AutoBudgetPlanRulesDto>(claimValue, JsonOptions);
            return AutoBudgetPlanRules.FromDto(dto);
        }
        catch (JsonException)
        {
            return AutoBudgetPlanRules.Default;
        }
    }

    private static decimal ResolveSeasonalityMultiplier(
        int month,
        BudgetPlanItem item,
        Dictionary<int, decimal> overrides,
        AutoBudgetPlanCategoryRule rules)
    {
        if (overrides.TryGetValue(item.CategoryId, out var overrideMultiplier))
            return ClampMultiplier(overrideMultiplier);

        return ClampMultiplier(rules.MonthCoefficients.GetValueOrDefault(month, 1m));
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

        return targetSeasonalityMultiplier > sourceSeasonalityMultiplier
            ? "Higher because of configured month coefficient."
            : "Lower because of configured month coefficient.";
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
        if (item.Category != null)
        {
            return item.Category.Priority switch
            {
                CategoryPriority.Mandatory => AutoCategoryPriority.Essential,
                CategoryPriority.Discretionary => AutoCategoryPriority.Discretionary,
                _ => AutoCategoryPriority.Flexible
            };
        }

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
        decimal financialStateTrend,
        bool protectedFromCuts,
        bool aggressiveCut)
    {
        if (protectedFromCuts)
            return 1m;

        if (financialStateTrend >= -0.03m)
            return 1m;

        var deterioration = ClampRate(Math.Abs(financialStateTrend) / 0.35m);
        var cutFactor = aggressiveCut ? 1.35m : 1m;
        var reduction = priority switch
        {
            AutoCategoryPriority.Essential => deterioration * 0.06m,
            AutoCategoryPriority.Flexible => deterioration * 0.16m,
            AutoCategoryPriority.Discretionary => deterioration * 0.32m,
            _ => deterioration * 0.16m
        } * cutFactor;

        return Math.Round(Math.Max(aggressiveCut ? 0.55m : 0.68m, 1m - reduction), 4);
    }

    private static decimal ApplyLimitRules(
        decimal amount,
        decimal recurringFloor,
        AutoBudgetPlanCategoryRule rules)
    {
        var result = amount;

        if (rules.MinimumLimit.HasValue && result > 0m)
            result = Math.Max(result, rules.MinimumLimit.Value);

        if (rules.MaximumLimit.HasValue)
            result = Math.Min(result, Math.Max(rules.MaximumLimit.Value, recurringFloor));

        return Math.Round(Math.Max(MinGeneratedAmount, result), 2);
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
            if (item.ProtectedFromCuts)
                maxCutRate = 0m;
            else if (item.AggressiveCut)
                maxCutRate = Math.Max(maxCutRate, item.Priority == AutoCategoryPriority.Essential ? 0.20m : 0.50m);

            var recurringFloor = Math.Max(0m, item.RecurringCommittedAmount);
            var cuttableAmount = Math.Max(0m, item.RecommendedAmount - recurringFloor);
            var maxCutAmount = Math.Round(Math.Min(item.RecommendedAmount * maxCutRate, cuttableAmount), 2);
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

        if (item.RecurringCommittedAmount > 0m)
            parts.Add("Includes scheduled recurring payments.");

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private sealed class RecurringBudgetCommitment
    {
        public int CategoryId { get; set; }
        public string CategoryTitle { get; set; } = null!;
        public Category? Category { get; set; }
        public int CurrencyId { get; set; }
        public decimal Amount { get; set; }
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
        public bool ProtectedFromCuts { get; set; }
        public bool AggressiveCut { get; set; }
        public decimal SeasonalityMultiplier { get; set; }
        public decimal RecurringCommittedAmount { get; set; }
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

    private sealed class AutoBudgetPlanRules
    {
        public static AutoBudgetPlanRules Default => FromDto(null);

        private readonly Dictionary<int, AutoBudgetPlanCategoryRule> _categoryRules;

        private AutoBudgetPlanRules(Dictionary<int, AutoBudgetPlanCategoryRule> categoryRules)
        {
            _categoryRules = categoryRules;
        }

        public AutoBudgetPlanCategoryRule GetCategoryRule(int categoryId)
        {
            return _categoryRules.TryGetValue(categoryId, out var rule)
                ? rule
                : AutoBudgetPlanCategoryRule.Default(categoryId);
        }

        public static AutoBudgetPlanRules FromDto(AutoBudgetPlanRulesDto? dto)
        {
            var categoryRules = (dto?.CategoryRules ?? [])
                .Where(rule => rule.CategoryId > 0)
                .GroupBy(rule => rule.CategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => AutoBudgetPlanCategoryRule.FromDto(group.Last()));

            return new AutoBudgetPlanRules(categoryRules);
        }
    }

    private sealed class AutoBudgetPlanCategoryRule
    {
        public int CategoryId { get; init; }
        public Dictionary<int, decimal> MonthCoefficients { get; init; } = new();
        public decimal? MinimumLimit { get; init; }
        public decimal? MaximumLimit { get; init; }
        public AutoBudgetPlanCutBehavior CutBehavior { get; init; } = AutoBudgetPlanCutBehavior.Normal;

        public static AutoBudgetPlanCategoryRule Default(int categoryId)
        {
            return FromDto(new AutoBudgetPlanCategoryRuleDto { CategoryId = categoryId });
        }

        public static AutoBudgetPlanCategoryRule FromDto(AutoBudgetPlanCategoryRuleDto dto)
        {
            var monthCoefficients = AutoBudgetPlanRulesDefaults.CreateMonthCoefficients()
                .ToDictionary(i => i.Month, i => i.Multiplier);

            foreach (var item in (dto.MonthCoefficients ?? []).Where(i => i.Month is >= 1 and <= 12))
                monthCoefficients[item.Month] = ClampMultiplier(item.Multiplier);

            var cutBehavior = (dto.CutBehavior ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "protected" => AutoBudgetPlanCutBehavior.Protected,
                "aggressive" => AutoBudgetPlanCutBehavior.Aggressive,
                _ => AutoBudgetPlanCutBehavior.Normal
            };

            return new AutoBudgetPlanCategoryRule
            {
                CategoryId = dto.CategoryId,
                MonthCoefficients = monthCoefficients,
                MinimumLimit = dto.MinimumLimit is >= 0m ? dto.MinimumLimit : null,
                MaximumLimit = dto.MaximumLimit is >= 0m ? dto.MaximumLimit : null,
                CutBehavior = cutBehavior
            };
        }
    }

    private enum AutoBudgetPlanCutBehavior
    {
        Normal,
        Protected,
        Aggressive
    }
}
