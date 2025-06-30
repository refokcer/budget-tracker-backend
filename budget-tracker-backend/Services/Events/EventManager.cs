namespace budget_tracker_backend.Services.Events;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using Microsoft.EntityFrameworkCore;

public class EventManager : IEventManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public EventManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Events
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Events.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Event?> GetByIdWithTransactionsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Include(e => e.Transactions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Event> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Event>(dto) ??
            throw new CustomException("Cannot map CreateEventDto", StatusCodes.Status400BadRequest);

        await _context.Events.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create event", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<Event> UpdateAsync(EventDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.Events.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Event not found", StatusCodes.Status404NotFound);

        _mapper.Map(dto, existing);
        _context.Events.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update event", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            throw new CustomException("Event not found", StatusCodes.Status404NotFound);

        _context.Events.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete event", StatusCodes.Status500InternalServerError);

        return true;
    }
}
