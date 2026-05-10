namespace budget_tracker_backend.Services.Algorithms.FinancialGoals;

using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Algorithms;

public interface IFinancialGoalForecastAlgorithm : IFinancialAlgorithm
{
    Task<FinancialGoalForecastDto> CalculateAsync(
        FinancialGoal goal,
        CancellationToken cancellationToken);
}
