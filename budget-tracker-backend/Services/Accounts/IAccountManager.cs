namespace budget_tracker_backend.Services.Accounts;

using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

public interface IAccountManager
{
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Account> CreateAsync(CreateAccountDto dto, CancellationToken cancellationToken);
    Task<Account> UpdateAsync(AccountDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task ApplyBalanceAsync(
        TransactionCategoryType type,
        decimal amount,
        Account? from,
        Account? to,
        bool reverse,
        CancellationToken cancellationToken);
}
