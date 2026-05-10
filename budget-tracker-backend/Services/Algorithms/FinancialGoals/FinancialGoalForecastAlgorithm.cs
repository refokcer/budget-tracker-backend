namespace budget_tracker_backend.Services.Algorithms.FinancialGoals;

using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Algorithms;
using Microsoft.EntityFrameworkCore;

public class FinancialGoalForecastAlgorithm : IFinancialGoalForecastAlgorithm
{
    private const int ForecastWindowMonths = 6;
    private const int AdjustmentWindowMonths = 3;

    private readonly IApplicationDbContext _context;
    private readonly IFinancialGoalBudgetAdjustmentAlgorithm _budgetAdjustmentAlgorithm;

    public FinancialGoalForecastAlgorithm(
        IApplicationDbContext context,
        IFinancialGoalBudgetAdjustmentAlgorithm budgetAdjustmentAlgorithm)
    {
        _context = context;
        _budgetAdjustmentAlgorithm = budgetAdjustmentAlgorithm;
    }

    public string Code => "financial-goal-forecast";
    public string Name => "Financial Goal Forecast";

    public async Task<FinancialGoalForecastDto> CalculateAsync(
        FinancialGoal goal,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentMonthEnd = currentMonthStart.AddMonths(1);
        var analysisStart = currentMonthStart.AddMonths(-(ForecastWindowMonths - 1));
        var adjustmentWindowStart = currentMonthStart.AddMonths(-(AdjustmentWindowMonths - 1));

        var transactions = await _context.Transactions
            .Where(t => t.Date >= analysisStart && t.Date < currentMonthEnd && t.CategoryId != null)
            .Include(t => t.Category)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var savingsAccounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Type == AccountType.Savings
                || a.Type == AccountType.Deposit
                || a.Type == AccountType.Investment)
            .ToListAsync(cancellationToken);

        var currentSavedAmount = goal.InitialAmount + ResolveTrackedSavingsAmount(goal, savingsAccounts);
        var remainingAmount = Math.Max(0m, goal.TargetAmount - currentSavedAmount);
        var monthsRemaining = GetMonthsRemaining(now, goal.TargetDate);

        var monthStarts = Enumerable.Range(0, ForecastWindowMonths)
            .Select(analysisStart.AddMonths)
            .ToList();

        var monthlyIncome = monthStarts.Select(month =>
        {
            var nextMonth = month.AddMonths(1);
            return transactions
                .Where(t => t.Date >= month && t.Date < nextMonth && t.Type == TransactionCategoryType.Income)
                .Sum(t => t.Amount);
        }).ToList();

        var monthlyExpenses = monthStarts.Select(month =>
        {
            var nextMonth = month.AddMonths(1);
            return transactions
                .Where(t => t.Date >= month && t.Date < nextMonth && t.Type == TransactionCategoryType.Expense)
                .Sum(t => t.Amount);
        }).ToList();

        var monthlyGoalContributions = monthStarts.Select(month =>
        {
            var nextMonth = month.AddMonths(1);
            return transactions
                .Where(t => t.Date >= month && t.Date < nextMonth)
                .Where(t => IsContributionToGoal(goal, t, savingsAccounts))
                .Sum(t => t.Amount);
        }).ToList();

        var averageMonthlyIncome = monthlyIncome.Average();
        var averageMonthlyExpenses = monthlyExpenses.Average();
        var averageMonthlyNetSavings = Math.Max(0m, averageMonthlyIncome - averageMonthlyExpenses);
        var averageMonthlyGoalContribution = monthlyGoalContributions.Average();
        var projectedMonthlyContribution = Math.Max(averageMonthlyNetSavings, averageMonthlyGoalContribution);
        var requiredMonthlyContribution = monthsRemaining > 0
            ? remainingAmount / monthsRemaining
            : remainingAmount;
        var contributionGap = Math.Max(0m, requiredMonthlyContribution - projectedMonthlyContribution);
        var forecastedAmountAtTargetDate = currentSavedAmount + projectedMonthlyContribution * monthsRemaining;
        var isAchievable = remainingAmount == 0m || forecastedAmountAtTargetDate >= goal.TargetAmount;

        var totalDurationMonths = Math.Max(1, GetMonthsBetween(goal.CreatedAt, goal.TargetDate));
        var elapsedMonths = Math.Min(totalDurationMonths, GetMonthsBetween(goal.CreatedAt, now));
        var expectedSavedAmountByNow = goal.InitialAmount
            + Math.Max(0m, goal.TargetAmount - goal.InitialAmount) * elapsedMonths / totalDurationMonths;
        var isOffTrack = currentSavedAmount + 0.01m < expectedSavedAmountByNow;

        var suggestions = await _budgetAdjustmentAlgorithm.SuggestAsync(
            goal,
            contributionGap,
            currentMonthStart,
            currentMonthEnd,
            adjustmentWindowStart,
            transactions,
            cancellationToken);

        var warnings = BuildWarnings(goal, monthsRemaining, isAchievable, isOffTrack, contributionGap, suggestions.Count);

        return new FinancialGoalForecastDto
        {
            GoalId = goal.Id,
            GoalTitle = goal.Title,
            TargetAmount = goal.TargetAmount,
            CurrentSavedAmount = Math.Round(currentSavedAmount, 2),
            RemainingAmount = Math.Round(remainingAmount, 2),
            ProgressRatio = goal.TargetAmount > 0m
                ? Math.Round(AlgorithmHelpers.Clamp(currentSavedAmount / goal.TargetAmount), 4)
                : 0m,
            MonthsRemaining = monthsRemaining,
            RequiredMonthlyContribution = Math.Round(requiredMonthlyContribution, 2),
            ProjectedMonthlyContribution = Math.Round(projectedMonthlyContribution, 2),
            ContributionGap = Math.Round(contributionGap, 2),
            ForecastedAmountAtTargetDate = Math.Round(forecastedAmountAtTargetDate, 2),
            ProjectedCompletionDate = GetProjectedCompletionDate(currentMonthStart, currentSavedAmount, goal.TargetAmount, projectedMonthlyContribution),
            ExpectedSavedAmountByNow = Math.Round(expectedSavedAmountByNow, 2),
            IsAchievable = isAchievable,
            IsOffTrack = isOffTrack,
            RiskLevel = GetRiskLevel(remainingAmount, isAchievable, isOffTrack, contributionGap, requiredMonthlyContribution),
            Warnings = warnings,
            SuggestedBudgetAdjustments = suggestions
        };
    }

    private static decimal ResolveTrackedSavingsAmount(FinancialGoal goal, List<Account> savingsAccounts)
    {
        if (goal.LinkedAccountId.HasValue)
            return goal.LinkedAccount?.Amount ?? 0m;

        return savingsAccounts.Sum(a => a.Amount);
    }

    private static bool IsContributionToGoal(FinancialGoal goal, Transaction transaction, List<Account> savingsAccounts)
    {
        if (goal.LinkedAccountId.HasValue)
        {
            return transaction.AccountTo == goal.LinkedAccountId.Value
                && transaction.Type is TransactionCategoryType.Transaction or TransactionCategoryType.Income;
        }

        var savingsAccountIds = savingsAccounts.Select(a => a.Id).ToHashSet();
        return transaction.AccountTo != null
            && savingsAccountIds.Contains(transaction.AccountTo.Value)
            && transaction.Type is TransactionCategoryType.Transaction or TransactionCategoryType.Income;
    }

    private static List<string> BuildWarnings(
        FinancialGoal goal,
        int monthsRemaining,
        bool isAchievable,
        bool isOffTrack,
        decimal contributionGap,
        int adjustmentCount)
    {
        var warnings = new List<string>();

        if (monthsRemaining == 0 && goal.TargetAmount > 0m)
            warnings.Add("Target date is too close to accumulate the remaining amount under the current plan.");

        if (!isAchievable)
            warnings.Add("Current income, expenses, and savings pace indicate a risk of not reaching the goal on time.");

        if (isOffTrack)
            warnings.Add("Actual savings are lagging behind the expected accumulation schedule.");

        if (contributionGap > 0m && adjustmentCount == 0)
            warnings.Add("No sufficient secondary spending categories were found for adaptive budget reduction.");

        return warnings;
    }

    private static string GetRiskLevel(
        decimal remainingAmount,
        bool isAchievable,
        bool isOffTrack,
        decimal contributionGap,
        decimal requiredMonthlyContribution)
    {
        if (remainingAmount <= 0m)
            return "Completed";

        if (!isAchievable && (requiredMonthlyContribution == 0m || contributionGap >= requiredMonthlyContribution * 0.5m))
            return "High";

        if (!isAchievable || isOffTrack)
            return "Medium";

        return "Low";
    }

    private static DateTime? GetProjectedCompletionDate(
        DateTime currentMonthStart,
        decimal currentSavedAmount,
        decimal targetAmount,
        decimal projectedMonthlyContribution)
    {
        var remainingAmount = targetAmount - currentSavedAmount;
        if (remainingAmount <= 0m)
            return currentMonthStart;

        if (projectedMonthlyContribution <= 0m)
            return null;

        var monthsNeeded = (int)Math.Ceiling(remainingAmount / projectedMonthlyContribution);
        return currentMonthStart.AddMonths(monthsNeeded);
    }

    private static int GetMonthsRemaining(DateTime now, DateTime targetDate)
    {
        if (targetDate <= now)
            return 0;

        var months = GetMonthsBetween(now, targetDate);
        return Math.Max(1, months);
    }

    private static int GetMonthsBetween(DateTime start, DateTime end)
    {
        if (end <= start)
            return 0;

        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (end.Day > start.Day)
            months++;

        return Math.Max(0, months);
    }
}
