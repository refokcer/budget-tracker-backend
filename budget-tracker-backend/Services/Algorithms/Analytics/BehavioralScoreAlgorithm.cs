namespace budget_tracker_backend.Services.Algorithms.Analytics;

using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Algorithms;
using Microsoft.EntityFrameworkCore;

public class BehavioralScoreAlgorithm : IBehavioralScoreAlgorithm
{
    private readonly IApplicationDbContext _context;

    public BehavioralScoreAlgorithm(IApplicationDbContext context)
    {
        _context = context;
    }

    public string Code => "behavioral-financial-score";
    public string Name => "Behavioral Financial Score";

    public async Task<BehavioralScoreDto> CalculateAsync(
        IEnumerable<Account> accounts,
        DateTime currentMonthStart,
        CancellationToken cancellationToken)
    {
        var accountList = accounts.ToList();
        var analysisStart = currentMonthStart.AddMonths(-5);
        var analysisEnd = currentMonthStart.AddMonths(1);
        var monthStarts = Enumerable.Range(0, 6)
            .Select(analysisStart.AddMonths)
            .ToList();

        var transactions = await _context.Transactions
            .Where(t => t.Date >= analysisStart && t.Date < analysisEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var plans = await _context.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.StartDate < analysisEnd && p.EndDate >= analysisStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var planItems = plans
            .SelectMany(plan => (plan.Items ?? [])
                .Where(item => item.Amount > 0m)
                .Select(item => new BehavioralPlanItem(
                    plan.Id,
                    plan.StartDate,
                    plan.EndDate,
                    item.CategoryId,
                    item.Amount)))
            .ToList();

        var expenses = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense)
            .ToList();

        var spentByPlanCategory = expenses
            .Where(t => t.BudgetPlanId != null && t.CategoryId != null)
            .GroupBy(t => new { PlanId = t.BudgetPlanId!.Value, CategoryId = t.CategoryId!.Value })
            .ToDictionary(g => (g.Key.PlanId, g.Key.CategoryId), g => g.Sum(t => t.Amount));

        var limitScores = planItems.Select(item =>
        {
            spentByPlanCategory.TryGetValue((item.PlanId, item.CategoryId), out var spent);
            return spent <= item.Amount || spent == 0m
                ? 1m
                : AlgorithmHelpers.Clamp(item.Amount / spent);
        }).ToList();

        var limitAdherence = limitScores.Count > 0
            ? limitScores.Average()
            : 1m;
        var overspentCategories = limitScores.Count(score => score < 1m);

        var plannedCategoryKeys = planItems
            .Select(item => (item.PlanId, item.CategoryId))
            .ToHashSet();

        var averageExpenseAmount = expenses.Count > 0
            ? expenses.Average(t => t.Amount)
            : 0m;
        var averageMonthlyExpenses = monthStarts
            .Select(month =>
            {
                var nextMonth = month.AddMonths(1);
                return expenses
                    .Where(t => t.Date >= month && t.Date < nextMonth)
                    .Sum(t => t.Amount);
            })
            .DefaultIfEmpty(0m)
            .Average();
        var categoryAverageAmounts = expenses
            .Where(t => t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Average(t => t.Amount));

        var impulsiveTransactions = expenses
            .Where(t => IsImpulsiveExpense(
                t,
                plannedCategoryKeys,
                categoryAverageAmounts,
                averageExpenseAmount,
                averageMonthlyExpenses))
            .ToList();

        var totalExpenseAmount = expenses.Sum(t => t.Amount);
        var impulsiveAmount = impulsiveTransactions.Sum(t => t.Amount);
        var impulsiveAmountShare = totalExpenseAmount > 0m
            ? AlgorithmHelpers.Clamp(impulsiveAmount / totalExpenseAmount)
            : 0m;
        var impulseControl = 1m - AlgorithmHelpers.Clamp(impulsiveAmountShare / 0.35m);

        var savingsAccountIds = accountList
            .Where(AlgorithmHelpers.IsSavingsAccount)
            .Select(a => a.Id)
            .ToHashSet();

        var savingMonths = 0;
        var activeIncomeMonths = 0;
        foreach (var month in monthStarts)
        {
            var nextMonth = month.AddMonths(1);
            var monthTransactions = transactions
                .Where(t => t.Date >= month && t.Date < nextMonth)
                .ToList();
            var income = monthTransactions
                .Where(t => t.Type == TransactionCategoryType.Income)
                .Sum(t => t.Amount);
            var expense = monthTransactions
                .Where(t => t.Type == TransactionCategoryType.Expense)
                .Sum(t => t.Amount);

            if (income <= 0m)
                continue;

            activeIncomeMonths++;

            var hasSavingsAction = monthTransactions.Any(t =>
                (t.Type == TransactionCategoryType.Transfer || t.Type == TransactionCategoryType.Income)
                && t.AccountTo != null
                && savingsAccountIds.Contains(t.AccountTo.Value));
            var hasMeaningfulPositiveCashFlow = income > 0m && (income - expense) / income >= 0.10m;

            if (hasSavingsAction || hasMeaningfulPositiveCashFlow)
                savingMonths++;
        }

        var savingsRegularity = activeIncomeMonths > 0
            ? AlgorithmHelpers.Clamp((decimal)savingMonths / activeIncomeMonths)
            : 0m;

        var warningResponse = CalculateWarningResponseScore(
            planItems,
            expenses,
            currentMonthStart,
            analysisEnd,
            out var warningEvents,
            out var resolvedWarningEvents);

        var score = (int)Math.Round(100m * (
            limitAdherence * 0.35m
            + impulseControl * 0.25m
            + savingsRegularity * 0.25m
            + warningResponse * 0.15m));
        score = Math.Clamp(score, 0, 100);

        var metrics = new BehavioralScoreMetricsDto
        {
            LimitAdherence = Math.Round(limitAdherence, 4),
            ImpulseControl = Math.Round(impulseControl, 4),
            SavingsRegularity = Math.Round(savingsRegularity, 4),
            WarningResponse = Math.Round(warningResponse, 4),
            PlannedCategoriesChecked = planItems.Count,
            OverspentCategories = overspentCategories,
            ImpulsiveTransactions = impulsiveTransactions.Count,
            ImpulsiveAmountShare = Math.Round(impulsiveAmountShare, 4),
            SavingMonths = savingMonths,
            ActiveIncomeMonths = activeIncomeMonths,
            WarningEvents = warningEvents,
            ResolvedWarningEvents = resolvedWarningEvents
        };

        return new BehavioralScoreDto
        {
            Score = score,
            Level = GetBehavioralScoreLevel(score),
            Metrics = metrics,
            Insights = BuildBehavioralScoreInsights(metrics)
        };
    }

    private static bool IsImpulsiveExpense(
        Transaction transaction,
        HashSet<(int PlanId, int CategoryId)> plannedCategoryKeys,
        Dictionary<int, decimal> categoryAverageAmounts,
        decimal averageExpenseAmount,
        decimal averageMonthlyExpenses)
    {
        var isOutsideBudget = transaction.BudgetPlanId == null
            || transaction.CategoryId == null
            || !plannedCategoryKeys.Contains((transaction.BudgetPlanId.Value, transaction.CategoryId.Value));

        categoryAverageAmounts.TryGetValue(transaction.CategoryId ?? 0, out var categoryAverage);
        var baseline = categoryAverage > 0m
            ? categoryAverage
            : averageExpenseAmount;
        var largeRelativeToHistory = baseline > 0m && transaction.Amount >= baseline * 1.75m;
        var largeRelativeToMonth = averageMonthlyExpenses > 0m
            && transaction.Amount >= averageMonthlyExpenses * 0.08m;

        return (isOutsideBudget && transaction.Amount >= Math.Max(averageExpenseAmount * 0.5m, 1m))
            || (largeRelativeToHistory && largeRelativeToMonth);
    }

    private static decimal CalculateWarningResponseScore(
        List<BehavioralPlanItem> planItems,
        List<Transaction> expenses,
        DateTime currentMonthStart,
        DateTime analysisEnd,
        out int warningEvents,
        out int resolvedWarningEvents)
    {
        warningEvents = 0;
        resolvedWarningEvents = 0;

        foreach (var item in planItems.Where(i => i.EndDate < currentMonthStart))
        {
            var spent = expenses
                .Where(t => t.BudgetPlanId == item.PlanId && t.CategoryId == item.CategoryId)
                .Sum(t => t.Amount);

            if (spent <= item.Amount)
                continue;

            var nextMonthStart = new DateTime(
                item.StartDate.Year,
                item.StartDate.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc).AddMonths(1);
            var nextMonthEnd = nextMonthStart.AddMonths(1);
            if (nextMonthEnd > analysisEnd)
                continue;

            warningEvents++;

            var nextLimit = planItems
                .Where(i => i.CategoryId == item.CategoryId
                    && i.StartDate < nextMonthEnd
                    && i.EndDate >= nextMonthStart)
                .Sum(i => i.Amount);
            var nextSpent = expenses
                .Where(t => t.CategoryId == item.CategoryId
                    && t.Date >= nextMonthStart
                    && t.Date < nextMonthEnd)
                .Sum(t => t.Amount);

            if ((nextLimit > 0m && nextSpent <= nextLimit) || nextSpent <= spent * 0.90m)
                resolvedWarningEvents++;
        }

        if (warningEvents == 0)
            return 1m;

        return AlgorithmHelpers.Clamp((decimal)resolvedWarningEvents / warningEvents);
    }

    private static List<string> BuildBehavioralScoreInsights(BehavioralScoreMetricsDto metrics)
    {
        var insights = new List<string>();

        if (metrics.LimitAdherence < 0.75m)
            insights.Add("Budget limits are often exceeded. Review limits or reduce categories with repeated overspending.");

        if (metrics.ImpulseControl < 0.75m)
            insights.Add("A noticeable share of spending happens outside the plan or as unusually large purchases.");

        if (metrics.SavingsRegularity < 0.67m)
            insights.Add("Savings are irregular. Add a recurring transfer to a savings account for every income month.");

        if (metrics.WarningResponse < 0.70m)
            insights.Add("Overspending patterns repeat after warnings. Rebalance the next month immediately after a limit is exceeded.");

        if (insights.Count == 0)
            insights.Add("Behavioral discipline is strong: limits, savings rhythm, and reaction to overspending are healthy.");

        return insights;
    }

    private static string GetBehavioralScoreLevel(int score)
    {
        if (score < 40)
            return "Reactive";

        if (score < 70)
            return "Developing";

        if (score < 85)
            return "Disciplined";

        return "Excellent";
    }

    private sealed record BehavioralPlanItem(
        int PlanId,
        DateTime StartDate,
        DateTime EndDate,
        int CategoryId,
        decimal Amount);
}

