namespace budget_tracker_backend.Services.BudgetPlans;

using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models;

public interface IBudgetPlanManager
{
    Task<IEnumerable<BudgetPlan>> GetAllAsync(CancellationToken cancellationToken);
    Task<BudgetPlan?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<BudgetPlan> CreateAsync(CreateBudgetPlanDto dto, CancellationToken cancellationToken);
    Task<BudgetPlan> UpdateAsync(BudgetPlanDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
