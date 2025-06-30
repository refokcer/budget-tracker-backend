namespace budget_tracker_backend.Services.Events;

using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

public interface IEventManager
{
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Event?> GetByIdWithTransactionsAsync(int id, CancellationToken cancellationToken);
    Task<Event> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken);
    Task<Event> UpdateAsync(EventDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
