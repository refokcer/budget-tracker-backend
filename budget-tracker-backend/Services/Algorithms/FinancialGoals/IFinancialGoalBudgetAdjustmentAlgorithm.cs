namespace budget_tracker_backend.Services.Algorithms.FinancialGoals;

using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Algorithms;

public interface IFinancialGoalBudgetAdjustmentAlgorithm : IFinancialAlgorithm
{
    Task<List<BudgetAdjustmentSuggestionDto>> SuggestAsync(
        FinancialGoal goal,
        decimal contributionGap,
        DateTime currentMonthStart,
        DateTime currentMonthEnd,
        DateTime adjustmentWindowStart,
        List<Transaction> transactions,
        CancellationToken cancellationToken);

    List<BudgetAdjustmentSuggestionDto> ResolveRequestedAdjustments(
        List<BudgetAdjustmentSuggestionDto> allowedSuggestions,
        List<BudgetAdjustmentSuggestionDto> requestedSuggestions);

    Task<BudgetPlan?> GetActiveMonthlyPlanAsync(
        DateTime currentMonthStart,
        DateTime currentMonthEnd,
        CancellationToken cancellationToken);

    Task<bool> HasAppliedAdjustmentAsync(
        string userId,
        int budgetPlanId,
        int categoryId,
        CancellationToken cancellationToken);

    string GetAppliedAdjustmentClaimType(int budgetPlanId, int categoryId);
}
