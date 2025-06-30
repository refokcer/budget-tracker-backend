namespace budget_tracker_backend.Services.Currencies;

using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

public interface ICurrencyManager
{
    Task<IEnumerable<Currency>> GetAllAsync(CancellationToken cancellationToken);
    Task<Currency?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Currency> CreateAsync(CreateCurrencyDto dto, CancellationToken cancellationToken);
    Task<Currency> UpdateAsync(CurrencyDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
