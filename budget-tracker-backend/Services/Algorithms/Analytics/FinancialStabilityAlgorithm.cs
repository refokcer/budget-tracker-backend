namespace budget_tracker_backend.Services.Algorithms.Analytics;

using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Algorithms;
using Microsoft.EntityFrameworkCore;

public class FinancialStabilityAlgorithm : IFinancialStabilityAlgorithm
{
    private readonly IApplicationDbContext _context;

    public FinancialStabilityAlgorithm(IApplicationDbContext context)
    {
        _context = context;
    }

    public string Code => "financial-stability-index";
    public string Name => "Financial Stability Index";

    public async Task<FinancialStabilityDto> CalculateAsync(
        IEnumerable<Account> accounts,
        decimal totalBalance,
        DateTime currentMonthStart,
        CancellationToken cancellationToken)
    {
        var accountList = accounts.ToList();
        var currentMonthEnd = currentMonthStart.AddMonths(1);
        var analysisStart = currentMonthStart.AddMonths(-5);
        var analysisEnd = currentMonthEnd;

        var transactions = await _context.Transactions
            .Where(t => t.Date >= analysisStart && t.Date < analysisEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var monthStarts = Enumerable.Range(0, 6)
            .Select(analysisStart.AddMonths)
            .ToList();

        var monthlyCashFlow = monthStarts
            .Select(month =>
            {
                var nextMonth = month.AddMonths(1);
                var monthTransactions = transactions
                    .Where(t => t.Date >= month && t.Date < nextMonth)
                    .ToList();

                return new
                {
                    Income = monthTransactions
                        .Where(t => t.Type == TransactionCategoryType.Income)
                        .Sum(t => t.Amount),
                    Expenses = monthTransactions
                        .Where(t => t.Type == TransactionCategoryType.Expense)
                        .Sum(t => t.Amount)
                };
            })
            .ToList();

        var currentMonthIncome = monthlyCashFlow[^1].Income;
        var currentMonthExpenses = monthlyCashFlow[^1].Expenses;
        var averageMonthlyIncome = monthlyCashFlow.Average(x => x.Income);
        var averageMonthlyExpenses = monthlyCashFlow.Average(x => x.Expenses);

        var activeMonthlyPlans = await _context.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < currentMonthEnd
                && p.EndDate >= currentMonthStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var mandatoryCategoryIds = activeMonthlyPlans
            .SelectMany(p => p.Items ?? [])
            .Select(i => i.CategoryId)
            .Distinct()
            .ToHashSet();

        var mandatoryExpenses = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date >= currentMonthStart
                && t.Date < currentMonthEnd
                && t.CategoryId != null
                && mandatoryCategoryIds.Contains(t.CategoryId.Value))
            .Sum(t => t.Amount);

        var savingsAccountIds = accountList
            .Where(AlgorithmHelpers.IsSavingsAccount)
            .Select(a => a.Id)
            .ToHashSet();

        var transfersToSavings = transactions
            .Where(t => t.Type == TransactionCategoryType.Transaction
                && t.Date >= currentMonthStart
                && t.Date < currentMonthEnd
                && t.AccountTo != null
                && savingsAccountIds.Contains(t.AccountTo.Value))
            .Sum(t => t.Amount);

        var netSavings = Math.Max(0m, currentMonthIncome - currentMonthExpenses);
        var mandatoryExpensesShare = currentMonthIncome > 0m
            ? AlgorithmHelpers.Clamp(mandatoryExpenses / currentMonthIncome)
            : 0m;
        var savingsShare = currentMonthIncome > 0m
            ? AlgorithmHelpers.Clamp(Math.Max(netSavings, transfersToSavings) / currentMonthIncome)
            : 0m;
        var emergencyFundMonths = averageMonthlyExpenses > 0m
            ? Math.Round(totalBalance / averageMonthlyExpenses, 2)
            : totalBalance > 0m ? 6m : 0m;

        var overspendingFrequency = await CalculateOverspendingFrequencyAsync(
            analysisStart,
            analysisEnd,
            cancellationToken);
        var incomeStability = CalculateIncomeStability(monthlyCashFlow.Select(x => x.Income));
        var goalAchievementIndex = await CalculateBudgetGoalAchievementIndexAsync(
            analysisStart,
            analysisEnd,
            cancellationToken);

        var mandatoryExpensesScore = 1m - mandatoryExpensesShare;
        var savingsScore = AlgorithmHelpers.Clamp(savingsShare / 0.2m);
        var emergencyFundScore = AlgorithmHelpers.Clamp(emergencyFundMonths / 6m);
        var overspendingScore = 1m - overspendingFrequency;

        var index = (int)Math.Round(100m * (
            mandatoryExpensesScore * 0.20m
            + savingsScore * 0.20m
            + emergencyFundScore * 0.20m
            + overspendingScore * 0.15m
            + incomeStability * 0.10m
            + goalAchievementIndex * 0.15m));

        index = Math.Clamp(index, 0, 100);

        var metrics = new FinancialStabilityMetricsDto
        {
            MandatoryExpensesShare = Math.Round(mandatoryExpensesShare, 4),
            SavingsShare = Math.Round(savingsShare, 4),
            EmergencyFundMonths = emergencyFundMonths,
            OverspendingFrequency = Math.Round(overspendingFrequency, 4),
            IncomeStability = Math.Round(incomeStability, 4),
            GoalAchievementIndex = Math.Round(goalAchievementIndex, 4),
            AverageMonthlyIncome = Math.Round(averageMonthlyIncome, 2),
            AverageMonthlyExpenses = Math.Round(averageMonthlyExpenses, 2)
        };

        return new FinancialStabilityDto
        {
            Index = index,
            Level = GetFinancialStabilityLevel(index),
            Metrics = metrics,
            Recommendations = BuildFinancialStabilityRecommendations(metrics)
        };
    }

    private async Task<decimal> CalculateOverspendingFrequencyAsync(
        DateTime analysisStart,
        DateTime analysisEnd,
        CancellationToken cancellationToken)
    {
        var plans = await _context.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.StartDate < analysisEnd && p.EndDate >= analysisStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (plans.Count == 0)
            return 0m;

        var planIds = plans.Select(p => p.Id).ToList();
        var spentByPlan = await _context.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.BudgetPlanId != null
                && planIds.Contains(t.BudgetPlanId.Value)
                && t.Date >= analysisStart
                && t.Date < analysisEnd)
            .GroupBy(t => t.BudgetPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Amount = g.Sum(t => t.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.PlanId, x => x.Amount, cancellationToken);

        var overspentPlans = plans.Count(plan =>
        {
            var plannedAmount = (plan.Items ?? []).Sum(i => i.Amount);
            if (plannedAmount <= 0m)
                return false;

            return spentByPlan.TryGetValue(plan.Id, out var spent) && spent > plannedAmount;
        });

        return AlgorithmHelpers.Clamp((decimal)overspentPlans / plans.Count);
    }

    private async Task<decimal> CalculateBudgetGoalAchievementIndexAsync(
        DateTime analysisStart,
        DateTime analysisEnd,
        CancellationToken cancellationToken)
    {
        var plans = await _context.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.StartDate < analysisEnd && p.EndDate >= analysisStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var planItems = plans
            .SelectMany(plan => (plan.Items ?? [])
                .Select(item => new
                {
                    PlanId = plan.Id,
                    item.CategoryId,
                    item.Amount
                }))
            .Where(item => item.Amount > 0m)
            .ToList();

        if (planItems.Count == 0)
            return 1m;

        var planIds = plans.Select(p => p.Id).ToList();
        var spentByPlanCategory = await _context.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.BudgetPlanId != null
                && t.CategoryId != null
                && planIds.Contains(t.BudgetPlanId.Value)
                && t.Date >= analysisStart
                && t.Date < analysisEnd)
            .GroupBy(t => new { PlanId = t.BudgetPlanId!.Value, CategoryId = t.CategoryId!.Value })
            .Select(g => new
            {
                g.Key.PlanId,
                g.Key.CategoryId,
                Amount = g.Sum(t => t.Amount)
            })
            .AsNoTracking()
            .ToDictionaryAsync(x => (x.PlanId, x.CategoryId), x => x.Amount, cancellationToken);

        var totalPlannedAmount = planItems.Sum(i => i.Amount);
        var weightedScore = planItems.Sum(item =>
        {
            spentByPlanCategory.TryGetValue((item.PlanId, item.CategoryId), out var spent);

            var categoryScore = spent <= item.Amount || spent == 0m
                ? 1m
                : AlgorithmHelpers.Clamp(item.Amount / spent);

            return categoryScore * item.Amount;
        });

        return totalPlannedAmount > 0m
            ? AlgorithmHelpers.Clamp(weightedScore / totalPlannedAmount)
            : 1m;
    }

    private static decimal CalculateIncomeStability(IEnumerable<decimal> monthlyIncomes)
    {
        var incomes = monthlyIncomes.ToList();
        var averageIncome = incomes.Average();

        if (averageIncome <= 0m)
            return 0m;

        var variance = incomes
            .Select(income => Math.Pow((double)(income - averageIncome), 2))
            .Average();
        var coefficientOfVariation = (decimal)(Math.Sqrt(variance) / (double)averageIncome);

        return 1m - AlgorithmHelpers.Clamp(coefficientOfVariation);
    }

    private static List<string> BuildFinancialStabilityRecommendations(FinancialStabilityMetricsDto metrics)
    {
        var recommendations = new List<string>();

        if (metrics.MandatoryExpensesShare > 0.5m)
            recommendations.Add("Reduce fixed expenses or review regular obligations.");

        if (metrics.SavingsShare < 0.1m)
            recommendations.Add("Save at least 10% of monthly income.");

        if (metrics.EmergencyFundMonths < 3m)
            recommendations.Add("Build an emergency fund that covers 3 to 6 months of expenses.");

        if (metrics.OverspendingFrequency > 0.25m)
            recommendations.Add("Review budget limits and track categories with repeated overspending.");

        if (metrics.IncomeStability < 0.6m)
            recommendations.Add("Plan the budget with conservative income estimates because income is unstable.");

        if (metrics.GoalAchievementIndex < 0.7m)
            recommendations.Add("Adjust category limits or reduce spending in categories with repeated overruns.");

        if (recommendations.Count == 0)
            recommendations.Add("Financial stability is high. Continue following the current budget discipline.");

        return recommendations;
    }

    private static string GetFinancialStabilityLevel(int index)
    {
        if (index < 40)
            return "Low";

        if (index < 70)
            return "Medium";

        return "High";
    }
}
