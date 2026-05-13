namespace budget_tracker_backend.Services.Algorithms.FinancialGoals;

using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class FinancialGoalBudgetAdjustmentAlgorithm : IFinancialGoalBudgetAdjustmentAlgorithm
{
    private const int AdjustmentWindowMonths = 3;
    private const decimal MaxSpendingReductionShare = 0.35m;
    private const decimal MaxBudgetLimitReductionShare = 0.30m;
    private const string AppliedAdjustmentClaimPrefix = "budget-adjustment-applied";

    private readonly IApplicationDbContext _context;

    public FinancialGoalBudgetAdjustmentAlgorithm(IApplicationDbContext context)
    {
        _context = context;
    }

    public string Code => "financial-goal-budget-adjustment";
    public string Name => "Financial Goal Budget Adjustment";

    public async Task<List<BudgetAdjustmentSuggestionDto>> SuggestAsync(
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
            .Where(i => i.Category!.Priority != CategoryPriority.Mandatory)
            .Select(i =>
            {
                expenseStatsByCategory.TryGetValue(i.CategoryId, out var stats);
                var averageMonthlySpending = stats?.AverageMonthlySpending ?? 0m;
                var currentMonthSpending = stats?.CurrentMonthSpending ?? 0m;
                var currentBudgetLimit = Math.Round(i.Amount, 2);
                var spendingBaseline = averageMonthlySpending > 0m
                    ? averageMonthlySpending
                    : currentBudgetLimit;
                var maxSpendingReductionShare = i.Category!.Priority == CategoryPriority.Discretionary
                    ? MaxSpendingReductionShare
                    : 0.20m;
                var maxBudgetLimitReductionShare = i.Category.Priority == CategoryPriority.Discretionary
                    ? MaxBudgetLimitReductionShare
                    : 0.18m;
                var maxReducible = Math.Min(
                    Math.Round(spendingBaseline * maxSpendingReductionShare, 2),
                    Math.Round(currentBudgetLimit * maxBudgetLimitReductionShare, 2));
                maxReducible = Math.Min(maxReducible, Math.Max(0m, currentBudgetLimit - currentMonthSpending));

                return new
                {
                    i.CategoryId,
                    CategoryTitle = i.Category!.Title,
                    AverageMonthlySpending = averageMonthlySpending,
                    CurrentMonthSpending = currentMonthSpending,
                    CurrentBudgetLimit = currentBudgetLimit,
                    MaxReducible = maxReducible,
                    Priority = i.Category.Priority
                };
            })
            .Where(x => x.CurrentBudgetLimit > 0m && x.MaxReducible > 0m)
            .OrderByDescending(x => x.Priority == CategoryPriority.Discretionary)
            .ThenByDescending(x => x.MaxReducible)
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

    public List<BudgetAdjustmentSuggestionDto> ResolveRequestedAdjustments(
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

    public Task<BudgetPlan?> GetActiveMonthlyPlanAsync(
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

    public async Task<bool> HasAppliedAdjustmentAsync(
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

    public string GetAppliedAdjustmentClaimType(int budgetPlanId, int categoryId)
    {
        return $"{GetAppliedAdjustmentClaimTypePrefix(budgetPlanId)}{categoryId}";
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
}
