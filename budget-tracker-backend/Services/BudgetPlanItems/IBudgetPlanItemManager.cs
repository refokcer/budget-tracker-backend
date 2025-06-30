namespace budget_tracker_backend.Services.BudgetPlanItems;

using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

public interface IBudgetPlanItemManager
{
    Task<IEnumerable<BudgetPlanItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<BudgetPlanItem?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<BudgetPlanItem>> GetByPlanIdAsync(int planId, CancellationToken cancellationToken);
    Task<BudgetPlanItem> CreateAsync(CreateBudgetPlanItemDto dto, CancellationToken cancellationToken);
    Task<BudgetPlanItem> UpdateAsync(BudgetPlanItemDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
