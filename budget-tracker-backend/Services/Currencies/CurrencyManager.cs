namespace budget_tracker_backend.Services.Currencies;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using Microsoft.EntityFrameworkCore;

public class CurrencyManager : ICurrencyManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CurrencyManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Currency>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Currencies
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Currency?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Currencies.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Currency> CreateAsync(CreateCurrencyDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Currency>(dto) ??
            throw new CustomException("Cannot map CreateCurrencyDto", StatusCodes.Status400BadRequest);

        await _context.Currencies.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create currency", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<Currency> UpdateAsync(CurrencyDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.Currencies.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Currency not found", StatusCodes.Status404NotFound);

        _mapper.Map(dto, existing);
        _context.Currencies.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update currency", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Currencies.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            throw new CustomException("Currency not found", StatusCodes.Status404NotFound);

        _context.Currencies.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete currency", StatusCodes.Status500InternalServerError);

        return true;
    }
}
