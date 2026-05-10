namespace budget_tracker_backend.Services.Algorithms.BudgetPlanning;

using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.Algorithms;

public interface IAutoBudgetPlanAlgorithm : IFinancialAlgorithm
{
    Task<AutoBudgetPlanResultDto> CreateMonthlyPlanAsync(
        AutoBudgetPlanRequestDto dto,
        CancellationToken cancellationToken);
}
