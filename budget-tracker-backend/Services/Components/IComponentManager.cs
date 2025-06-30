namespace budget_tracker_backend.Services.Components;

using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Models.Enums;

public interface IComponentManager
{
    Task<IncomeModalDto> GetIncomeModalAsync(CancellationToken cancellationToken);
    Task<ExpenseModalDto> GetExpenseModalAsync(CancellationToken cancellationToken);
    Task<TransferModalDto> GetTransferModalAsync(CancellationToken cancellationToken);
    Task<EditPlanModalDto> GetEditPlanModalAsync(CancellationToken cancellationToken);
    Task<ManageAccountsDto> GetManageAccountsAsync(CancellationToken cancellationToken);
    Task<ManageCategoriesDto> GetManageCategoriesAsync(TransactionCategoryType type, CancellationToken cancellationToken);
}
