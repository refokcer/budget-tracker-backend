using budget_tracker_backend.Dto.RecurringPayments;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Services.RecurringPayments;

public interface IRecurringPaymentManager
{
    Task<IEnumerable<RecurringPaymentDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<RecurringPaymentDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<RecurringPaymentDto> CreateAsync(CreateRecurringPaymentDto dto, CancellationToken cancellationToken);
    Task<RecurringPaymentDto> UpdateAsync(UpdateRecurringPaymentDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<RecurringPaymentOptionsDto> GetOptionsAsync(CancellationToken cancellationToken);
    Task<List<RecurringPaymentOccurrenceDto>> GetProjectedOccurrencesAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken);
    Task<GenerateRecurringPaymentsResultDto> GenerateDueTransactionsAsync(
        DateTime upTo,
        CancellationToken cancellationToken);
}
