namespace budget_tracker_backend.Services.Transactions;

using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

public interface ITransactionManager
{
    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken);
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetByBudgetPlanIdAsync(int planId, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetFilteredAsync(
        TransactionCategoryType? type,
        DateTime? startDate,
        DateTime? endDate,
        int? eventId,
        CancellationToken cancellationToken);

    Task<IEnumerable<Transaction>> GetFilteredDetailedAsync(
        TransactionCategoryType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        int? budgetPlanId,
        int? accountFrom,
        int? accountTo,
        CancellationToken cancellationToken);
    Task<Transaction> CreateAsync(CreateTransactionDto dto, CancellationToken cancellationToken);
    Task<Transaction> UpdateAsync(UpdateTransactionDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
