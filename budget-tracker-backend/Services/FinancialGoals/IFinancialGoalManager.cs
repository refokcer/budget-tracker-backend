using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Services.FinancialGoals;

public interface IFinancialGoalManager
{
    Task<IEnumerable<FinancialGoal>> GetAllAsync(CancellationToken cancellationToken);
    Task<FinancialGoal?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<FinancialGoal> CreateAsync(CreateFinancialGoalDto dto, CancellationToken cancellationToken);
    Task<FinancialGoal> UpdateAsync(FinancialGoalDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<FinancialGoalForecastDto> GetForecastAsync(int id, CancellationToken cancellationToken);
    Task<ApplyBudgetAdjustmentsResultDto> ApplyBudgetAdjustmentsAsync(int id, CancellationToken cancellationToken);
}
