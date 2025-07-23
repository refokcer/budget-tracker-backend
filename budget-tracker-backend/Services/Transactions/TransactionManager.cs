namespace budget_tracker_backend.Services.Transactions;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Accounts;
using Microsoft.EntityFrameworkCore;

public class TransactionManager : ITransactionManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAccountManager _accountManager;

    public TransactionManager(IApplicationDbContext context, IMapper mapper, IAccountManager accountManager)
    {
        _context = context;
        _mapper = mapper;
        _accountManager = accountManager;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Transactions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(t => t.EventId == eventId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByBudgetPlanIdAsync(int planId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(t => t.BudgetPlanId == planId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetFilteredAsync(
        TransactionCategoryType? type,
        DateTime? startDate,
        DateTime? endDate,
        int? eventId,
        CancellationToken cancellationToken)
    {
        var query = _context.Transactions.AsQueryable();

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);
        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);
        if (eventId.HasValue)
            query = query.Where(t => t.EventId == eventId.Value);

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Transaction> CreateAsync(CreateTransactionDto dto, CancellationToken token)
    {
        var entity = _mapper.Map<Transaction>(dto) ??
            throw new CustomException("Cannot map CreateTransactionDto", StatusCodes.Status400BadRequest);

        var type = dto.Type ?? TransactionCategoryType.None;
        entity.Type = type;
        if (type == TransactionCategoryType.None)
            throw new CustomException("Transaction type not defined", StatusCodes.Status400BadRequest);

        var result = await _accountManager.HandleTransactionAsync(
            type,
            entity.Amount,
            entity.AccountFrom,
            entity.AccountTo,
            false,
            token);
        if (result.IsFailed)
            throw new CustomException(result.Errors.First().Message, StatusCodes.Status400BadRequest);

        await _context.Transactions.AddAsync(entity, token);
        var saved = await _context.SaveChangesAsync(token) > 0;
        if (!saved)
            throw new CustomException("Failed to create transaction", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<Transaction> UpdateAsync(UpdateTransactionDto dto, CancellationToken ct)
    {
        var entity = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == dto.Id, ct);
        if (entity == null)
            throw new CustomException("Transaction not found", StatusCodes.Status404NotFound);

        var oldFrom = entity.AccountFrom.HasValue ? await _accountManager.GetByIdAsync(entity.AccountFrom.Value, ct) : null;
        var oldTo = entity.AccountTo.HasValue ? await _accountManager.GetByIdAsync(entity.AccountTo.Value, ct) : null;
        await _accountManager.ApplyBalanceAsync(entity.Type, entity.Amount, oldFrom, oldTo, true, ct);

        _mapper.Map(dto, entity);

        var newFrom = entity.AccountFrom.HasValue ? await _accountManager.GetByIdAsync(entity.AccountFrom.Value, ct) : null;
        var newTo = entity.AccountTo.HasValue ? await _accountManager.GetByIdAsync(entity.AccountTo.Value, ct) : null;
        if (entity.AccountFrom.HasValue && newFrom == null)
            throw new CustomException("AccountFrom not found", StatusCodes.Status400BadRequest);
        if (entity.AccountTo.HasValue && newTo == null)
            throw new CustomException("AccountTo not found", StatusCodes.Status400BadRequest);

        await _accountManager.ApplyBalanceAsync(entity.Type, entity.Amount, newFrom, newTo, false, ct);

        var saved = await _context.SaveChangesAsync(ct) > 0;
        if (!saved)
            throw new CustomException("Failed to update transaction", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null)
            throw new CustomException("Transaction not found", StatusCodes.Status404NotFound);

        var from = entity.AccountFrom.HasValue ? await _accountManager.GetByIdAsync(entity.AccountFrom.Value, ct) : null;
        var to = entity.AccountTo.HasValue ? await _accountManager.GetByIdAsync(entity.AccountTo.Value, ct) : null;
        await _accountManager.ApplyBalanceAsync(entity.Type, entity.Amount, from, to, true, ct);

        _context.Transactions.Remove(entity);
        var saved = await _context.SaveChangesAsync(ct) > 0;
        if (!saved)
            throw new CustomException("Failed to delete transaction", StatusCodes.Status500InternalServerError);

        return true;
    }
}
