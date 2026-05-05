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
    private const decimal MaxSpendingReductionShare = 0.35m;
    private const decimal MaxBudgetLimitReductionShare = 0.30m;
    private const string AppliedAdjustmentClaimPrefix = "budget-adjustment-applied";

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

    public async Task<ApplyBudgetAdjustmentsResultDto> ApplyBudgetAdjustmentsAsync(
        int id,
        ApplyBudgetAdjustmentsRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var goal = await GetRequiredGoalAsync(id, cancellationToken);
        var forecast = await BuildForecastAsync(goal, cancellationToken);
        var hasCustomAdjustments = dto?.Adjustments != null;

        if (!hasCustomAdjustments && forecast.SuggestedBudgetAdjustments.Count == 0)
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

        var activePlan = await GetActiveMonthlyPlanAsync(currentMonthStart, currentMonthEnd, cancellationToken);
        if (activePlan == null)
            throw new CustomException("No active monthly budget plan found for adaptive adjustments", StatusCodes.Status400BadRequest);

        var adjustmentsToApply = hasCustomAdjustments
            ? ResolveRequestedBudgetAdjustments(forecast.SuggestedBudgetAdjustments, dto!.Adjustments!)
            : forecast.SuggestedBudgetAdjustments;

        if (adjustmentsToApply.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = activePlan.Id,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var appliedSuggestions = new List<BudgetAdjustmentSuggestionDto>();
        foreach (var suggestion in adjustmentsToApply)
        {
            var existingItem = activePlan.Items?.FirstOrDefault(i => i.CategoryId == suggestion.CategoryId);
            if (existingItem == null)
                continue;

            if (await HasAppliedAdjustmentAsync(activePlan.UserId, activePlan.Id, suggestion.CategoryId, cancellationToken))
                continue;

            existingItem.Amount = suggestion.RecommendedBudgetLimit;
            await _context.UserClaims.AddAsync(new Microsoft.AspNetCore.Identity.IdentityUserClaim<string>
            {
                UserId = activePlan.UserId,
                ClaimType = GetAppliedAdjustmentClaimType(activePlan.Id, suggestion.CategoryId),
                ClaimValue = $"{goal.Id}|{suggestion.SuggestedReduction:0.##}|{DateTime.UtcNow:O}"
            }, cancellationToken);

            appliedSuggestions.Add(suggestion);
        }

        if (appliedSuggestions.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = activePlan.Id,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to apply adaptive budget adjustments", StatusCodes.Status500InternalServerError);

        return new ApplyBudgetAdjustmentsResultDto
        {
            GoalId = id,
            BudgetPlanId = activePlan.Id,
            AppliedAdjustmentsCount = appliedSuggestions.Count,
            AppliedAdjustments = appliedSuggestions,
            Forecast = forecast
        };
    }

    private static List<BudgetAdjustmentSuggestionDto> ResolveRequestedBudgetAdjustments(
        List<BudgetAdjustmentSuggestionDto> allowedSuggestions,
        List<BudgetAdjustmentSuggestionDto> requestedSuggestions)
    {
        if (requestedSuggestions.Count == 0)
            return new List<BudgetAdjustmentSuggestionDto>();

        var allowedByCategory = allowedSuggestions.ToDictionary(s => s.CategoryId);
        var requestedCategoryIds = new HashSet<int>();
        var resolved = new List<BudgetAdjustmentSuggestionDto>();

        foreach (var requested in requestedSuggestions)
        {
            if (!requestedCategoryIds.Add(requested.CategoryId))
                throw new CustomException("Duplicate budget adjustment category", StatusCodes.Status400BadRequest);

            if (!allowedByCategory.TryGetValue(requested.CategoryId, out var allowed))
                throw new CustomException("Budget adjustment is no longer available", StatusCodes.Status400BadRequest);

            var requestedLimit = Math.Round(requested.RecommendedBudgetLimit, 2);
            var minimumAllowedLimit = Math.Round(allowed.RecommendedBudgetLimit, 2);
            var currentLimit = Math.Round(allowed.CurrentBudgetLimit, 2);

            if (requestedLimit < minimumAllowedLimit || requestedLimit > currentLimit)
                throw new CustomException("Budget adjustment is outside allowed limits", StatusCodes.Status400BadRequest);

            var requestedReduction = Math.Round(currentLimit - requestedLimit, 2);
            if (requestedReduction <= 0m)
                continue;

            resolved.Add(new BudgetAdjustmentSuggestionDto
            {
                CategoryId = allowed.CategoryId,
                CategoryTitle = allowed.CategoryTitle,
                AverageMonthlySpending = allowed.AverageMonthlySpending,
                CurrentBudgetLimit = currentLimit,
                RecommendedBudgetLimit = requestedLimit,
                SuggestedReduction = requestedReduction
            });
        }

        return resolved;
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
        FinancialGoal goal,
        decimal contributionGap,
        DateTime currentMonthStart,
        DateTime currentMonthEnd,
        DateTime adjustmentWindowStart,
        List<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        if (contributionGap <= 0m)
            return new List<BudgetAdjustmentSuggestionDto>();

        var activePlan = await GetActiveMonthlyPlanAsync(currentMonthStart, currentMonthEnd, cancellationToken);
        if (activePlan?.Items == null)
            return new List<BudgetAdjustmentSuggestionDto>();

        var alreadyAdjustedCategoryIds = await GetAppliedAdjustmentCategoryIdsAsync(
            activePlan.UserId,
            activePlan.Id,
            cancellationToken);

        var expenseStatsByCategory = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date >= adjustmentWindowStart
                && t.Date < currentMonthEnd
                && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    AverageMonthlySpending = g.Sum(t => t.Amount) / AdjustmentWindowMonths,
                    CurrentMonthSpending = g.Where(t => t.Date >= currentMonthStart && t.Date < currentMonthEnd).Sum(t => t.Amount)
                });

        var candidateCategories = activePlan.Items
            .Where(i => i.Category != null)
            .Where(i => !alreadyAdjustedCategoryIds.Contains(i.CategoryId))
            .Where(i => !IsProtectedBudgetCategory(i.Category!.Title))
            .Select(i =>
            {
                expenseStatsByCategory.TryGetValue(i.CategoryId, out var stats);
                var averageMonthlySpending = stats?.AverageMonthlySpending ?? 0m;
                var currentMonthSpending = stats?.CurrentMonthSpending ?? 0m;
                var currentBudgetLimit = Math.Round(i.Amount, 2);
                var spendingBaseline = averageMonthlySpending > 0m
                    ? averageMonthlySpending
                    : currentBudgetLimit;
                var maxReducible = Math.Min(
                    Math.Round(spendingBaseline * MaxSpendingReductionShare, 2),
                    Math.Round(currentBudgetLimit * MaxBudgetLimitReductionShare, 2));
                maxReducible = Math.Min(maxReducible, Math.Max(0m, currentBudgetLimit - currentMonthSpending));

                return new
                {
                    i.CategoryId,
                    CategoryTitle = i.Category!.Title,
                    AverageMonthlySpending = averageMonthlySpending,
                    CurrentMonthSpending = currentMonthSpending,
                    CurrentBudgetLimit = currentBudgetLimit,
                    MaxReducible = maxReducible
                };
            })
            .Where(x => x.CurrentBudgetLimit > 0m && x.MaxReducible > 0m)
            .OrderByDescending(x => x.MaxReducible)
            .ThenByDescending(x => x.CurrentMonthSpending)
            .ThenByDescending(x => x.AverageMonthlySpending)
            .ToList();

        var suggestions = new List<BudgetAdjustmentSuggestionDto>();
        var remainingGap = contributionGap;

        foreach (var category in candidateCategories)
        {
            if (remainingGap <= 0m)
                break;

            var suggestedReduction = Math.Min(category.MaxReducible, remainingGap);
            if (suggestedReduction <= 0m)
                continue;

            var recommendedBudgetLimit = Math.Max(
                category.CurrentMonthSpending,
                category.CurrentBudgetLimit - suggestedReduction);

            suggestions.Add(new BudgetAdjustmentSuggestionDto
            {
                CategoryId = category.CategoryId,
                CategoryTitle = category.CategoryTitle,
                AverageMonthlySpending = Math.Round(category.AverageMonthlySpending, 2),
                CurrentBudgetLimit = category.CurrentBudgetLimit,
                RecommendedBudgetLimit = Math.Round(recommendedBudgetLimit, 2),
                SuggestedReduction = Math.Round(suggestedReduction, 2)
            });

            remainingGap -= suggestedReduction;
        }

        return suggestions;
    }

    private Task<BudgetPlan?> GetActiveMonthlyPlanAsync(
        DateTime currentMonthStart,
        DateTime currentMonthEnd,
        CancellationToken cancellationToken)
    {
        return _context.BudgetPlans
            .Include(p => p.Items)!
                .ThenInclude(i => i.Category)
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < currentMonthEnd
                && p.EndDate >= currentMonthStart)
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<HashSet<int>> GetAppliedAdjustmentCategoryIdsAsync(
        string userId,
        int budgetPlanId,
        CancellationToken cancellationToken)
    {
        var prefix = GetAppliedAdjustmentClaimTypePrefix(budgetPlanId);
        var claimTypes = await _context.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.ClaimType != null && c.ClaimType.StartsWith(prefix))
            .Select(c => c.ClaimType!)
            .ToListAsync(cancellationToken);

        return claimTypes
            .Select(type => int.TryParse(type[prefix.Length..], out var categoryId) ? categoryId : (int?)null)
            .Where(categoryId => categoryId.HasValue)
            .Select(categoryId => categoryId!.Value)
            .ToHashSet();
    }

    private async Task<bool> HasAppliedAdjustmentAsync(
        string userId,
        int budgetPlanId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        var claimType = GetAppliedAdjustmentClaimType(budgetPlanId, categoryId);
        return await _context.UserClaims
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.ClaimType == claimType, cancellationToken);
    }

    private static string GetAppliedAdjustmentClaimType(int budgetPlanId, int categoryId)
    {
        return $"{GetAppliedAdjustmentClaimTypePrefix(budgetPlanId)}{categoryId}";
    }

    private static string GetAppliedAdjustmentClaimTypePrefix(int budgetPlanId)
    {
        return $"{AppliedAdjustmentClaimPrefix}:{budgetPlanId}:";
    }

    private static bool IsProtectedBudgetCategory(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var protectedKeywords = new[]
        {
            "rent", "mortgage", "loan", "debt", "insurance", "tax",
            "utility", "utilities", "heating", "electric", "water", "gas",
            "communal", "medical", "health", "medicine", "tuition",
            "оренда", "аренда", "ипотека", "іпотека", "кредит", "долг", "борг",
            "страхов", "налог", "подат", "коммун", "комун", "отоп", "елект",
            "элект", "вода", "газ", "медиц", "ліки", "лекар", "навчан"
        };

        return protectedKeywords.Any(normalized.Contains);
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
