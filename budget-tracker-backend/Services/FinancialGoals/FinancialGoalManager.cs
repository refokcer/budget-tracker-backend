namespace budget_tracker_backend.Services.FinancialGoals;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class FinancialGoalManager : IFinancialGoalManager
{
    private const int ForecastWindowMonths = 6;
    private const int AdjustmentWindowMonths = 3;
    private const decimal MaxCategoryReductionShare = 0.35m;

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public FinancialGoalManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<FinancialGoal>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .AsNoTracking()
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialGoal?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<FinancialGoal> CreateAsync(CreateFinancialGoalDto dto, CancellationToken cancellationToken)
    {
        ValidateGoalDto(dto.Title, dto.TargetAmount, dto.InitialAmount, dto.TargetDate);
        await ValidateLinkedAccountAsync(dto.LinkedAccountId, cancellationToken);

        var entity = _mapper.Map<FinancialGoal>(dto)
            ?? throw new CustomException("Cannot map CreateFinancialGoalDto", StatusCodes.Status400BadRequest);

        entity.CreatedAt = DateTime.UtcNow;

        await _context.FinancialGoals.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create financial goal", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<FinancialGoal> UpdateAsync(FinancialGoalDto dto, CancellationToken cancellationToken)
    {
        ValidateGoalDto(dto.Title, dto.TargetAmount, dto.InitialAmount, dto.TargetDate);
        await ValidateLinkedAccountAsync(dto.LinkedAccountId, cancellationToken);

        var existing = await _context.FinancialGoals.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);

        existing.Title = dto.Title;
        existing.TargetAmount = dto.TargetAmount;
        existing.InitialAmount = dto.InitialAmount;
        existing.TargetDate = dto.TargetDate;
        existing.LinkedAccountId = dto.LinkedAccountId;
        existing.Description = dto.Description;

        _context.FinancialGoals.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update financial goal", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.FinancialGoals.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);

        _context.FinancialGoals.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete financial goal", StatusCodes.Status500InternalServerError);

        return true;
    }

    public async Task<FinancialGoalForecastDto> GetForecastAsync(int id, CancellationToken cancellationToken)
    {
        var goal = await GetRequiredGoalAsync(id, cancellationToken);
        return await BuildForecastAsync(goal, cancellationToken);
    }

    public async Task<ApplyBudgetAdjustmentsResultDto> ApplyBudgetAdjustmentsAsync(int id, CancellationToken cancellationToken)
    {
        var goal = await GetRequiredGoalAsync(id, cancellationToken);
        var forecast = await BuildForecastAsync(goal, cancellationToken);

        if (forecast.SuggestedBudgetAdjustments.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = null,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentMonthEnd = currentMonthStart.AddMonths(1);

        var activePlan = await _context.BudgetPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < currentMonthEnd
                && p.EndDate >= currentMonthStart, cancellationToken);
        if (activePlan == null)
            throw new CustomException("No active monthly budget plan found for adaptive adjustments", StatusCodes.Status400BadRequest);

        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);
        foreach (var suggestion in forecast.SuggestedBudgetAdjustments)
        {
            var existingItem = activePlan.Items?.FirstOrDefault(i => i.CategoryId == suggestion.CategoryId);
            if (existingItem != null)
            {
                existingItem.Amount = suggestion.RecommendedBudgetLimit;
                continue;
            }

            var item = new BudgetPlanItem
            {
                BudgetPlanId = activePlan.Id,
                CategoryId = suggestion.CategoryId,
                Amount = suggestion.RecommendedBudgetLimit,
                CurrencyId = baseCurrencyId,
                Description = $"Adaptive limit for financial goal \"{goal.Title}\""
            };

            await _context.BudgetPlanItems.AddAsync(item, cancellationToken);
        }

        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to apply adaptive budget adjustments", StatusCodes.Status500InternalServerError);

        return new ApplyBudgetAdjustmentsResultDto
        {
            GoalId = id,
            BudgetPlanId = activePlan.Id,
            AppliedAdjustmentsCount = forecast.SuggestedBudgetAdjustments.Count,
            AppliedAdjustments = forecast.SuggestedBudgetAdjustments,
            Forecast = forecast
        };
    }

    private async Task<FinancialGoal> GetRequiredGoalAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);
    }

    private async Task<FinancialGoalForecastDto> BuildForecastAsync(FinancialGoal goal, CancellationToken cancellationToken)
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

        var suggestions = await BuildBudgetAdjustmentSuggestionsAsync(
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
                ? Math.Round(Clamp(currentSavedAmount / goal.TargetAmount), 4)
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

    private async Task<List<BudgetAdjustmentSuggestionDto>> BuildBudgetAdjustmentSuggestionsAsync(
        decimal contributionGap,
        DateTime currentMonthStart,
        DateTime currentMonthEnd,
        DateTime adjustmentWindowStart,
        List<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        if (contributionGap <= 0m)
            return new List<BudgetAdjustmentSuggestionDto>();

        var activePlan = await _context.BudgetPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < currentMonthEnd
                && p.EndDate >= currentMonthStart, cancellationToken);

        var mandatoryCategoryIds = activePlan?.Items?
            .Select(i => i.CategoryId)
            .ToHashSet() ?? new HashSet<int>();

        var candidateCategories = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date >= adjustmentWindowStart
                && t.Date < currentMonthEnd
                && t.CategoryId != null
                && !mandatoryCategoryIds.Contains(t.CategoryId.Value))
            .GroupBy(t => new { CategoryId = t.CategoryId!.Value, CategoryTitle = t.Category!.Title })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryTitle,
                AverageMonthlySpending = g.Sum(t => t.Amount) / AdjustmentWindowMonths,
                CurrentMonthSpending = g.Where(t => t.Date >= currentMonthStart && t.Date < currentMonthEnd).Sum(t => t.Amount),
                CurrentBudgetLimit = activePlan?.Items?.FirstOrDefault(i => i.CategoryId == g.Key.CategoryId)?.Amount
                    ?? g.Sum(t => t.Amount) / AdjustmentWindowMonths
            })
            .Where(x => x.AverageMonthlySpending > 0m)
            .OrderByDescending(x => x.CurrentMonthSpending)
            .ThenByDescending(x => x.AverageMonthlySpending)
            .ToList();

        var suggestions = new List<BudgetAdjustmentSuggestionDto>();
        var remainingGap = contributionGap;

        foreach (var category in candidateCategories)
        {
            if (remainingGap <= 0m)
                break;

            var maxReducible = Math.Round(category.AverageMonthlySpending * MaxCategoryReductionShare, 2);
            var suggestedReduction = Math.Min(maxReducible, remainingGap);
            if (suggestedReduction <= 0m)
                continue;

            var currentBudgetLimit = Math.Round(category.CurrentBudgetLimit, 2);
            var recommendedBudgetLimit = Math.Max(0m, currentBudgetLimit - suggestedReduction);

            suggestions.Add(new BudgetAdjustmentSuggestionDto
            {
                CategoryId = category.CategoryId,
                CategoryTitle = category.CategoryTitle,
                AverageMonthlySpending = Math.Round(category.AverageMonthlySpending, 2),
                CurrentBudgetLimit = currentBudgetLimit,
                RecommendedBudgetLimit = Math.Round(recommendedBudgetLimit, 2),
                SuggestedReduction = Math.Round(suggestedReduction, 2)
            });

            remainingGap -= suggestedReduction;
        }

        return suggestions;
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var currencyId = await _context.Currencies
            .Where(c => c.IsBase)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (currencyId.HasValue)
            return currencyId.Value;

        currencyId = await _context.Currencies
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return currencyId ?? throw new CustomException("No currencies available to create adaptive budget limit", StatusCodes.Status400BadRequest);
    }

    private async Task ValidateLinkedAccountAsync(int? linkedAccountId, CancellationToken cancellationToken)
    {
        if (!linkedAccountId.HasValue)
            return;

        var linkedAccount = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == linkedAccountId.Value, cancellationToken);
        if (linkedAccount == null)
            throw new CustomException("Linked account not found", StatusCodes.Status400BadRequest);

        if (!IsSavingsAccount(linkedAccount))
            throw new CustomException("Linked account must be a savings, deposit, or investment account", StatusCodes.Status400BadRequest);
    }

    private static void ValidateGoalDto(string title, decimal targetAmount, decimal initialAmount, DateTime targetDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new CustomException("Financial goal title is required", StatusCodes.Status400BadRequest);

        if (targetAmount <= 0m)
            throw new CustomException("Target amount must be greater than zero", StatusCodes.Status400BadRequest);

        if (initialAmount < 0m)
            throw new CustomException("Initial amount cannot be negative", StatusCodes.Status400BadRequest);

        if (initialAmount > targetAmount)
            throw new CustomException("Initial amount cannot exceed target amount", StatusCodes.Status400BadRequest);

        if (targetDate.Date <= DateTime.UtcNow.Date)
            throw new CustomException("Target date must be in the future", StatusCodes.Status400BadRequest);
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

    private static bool IsSavingsAccount(Account account)
    {
        return account.Type is AccountType.Savings
            or AccountType.Deposit
            or AccountType.Investment;
    }

    private static decimal Clamp(decimal value)
    {
        return Math.Min(1m, Math.Max(0m, value));
    }
}
