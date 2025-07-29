namespace budget_tracker_backend.Services.Transactions;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Accounts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<IEnumerable<Transaction>> GetFilteredDetailedAsync(
        TransactionCategoryType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        int? budgetPlanId,
        int? accountFrom,
        int? accountTo,
        CancellationToken cancellationToken)
    {
        var query = _context.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.BudgetPlan)
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);
        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);
        if (budgetPlanId.HasValue)
            query = query.Where(t => t.BudgetPlanId == budgetPlanId.Value);
        if (accountFrom.HasValue)
            query = query.Where(t => t.AccountFrom == accountFrom.Value);
        if (accountTo.HasValue)
            query = query.Where(t => t.AccountTo == accountTo.Value);

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

        entity.UnicCode = GenerateUnicCode(entity.Amount, entity.Date, entity.AuthCode);

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

        if (dto.Title != null) entity.Title = dto.Title;
        if (dto.Amount.HasValue) entity.Amount = dto.Amount.Value;
        if (dto.AccountFrom.HasValue) entity.AccountFrom = dto.AccountFrom;
        if (dto.AccountTo.HasValue) entity.AccountTo = dto.AccountTo;
        if (dto.EventId.HasValue) entity.EventId = dto.EventId;
        if (dto.BudgetPlanId.HasValue) entity.BudgetPlanId = dto.BudgetPlanId;
        if (dto.CurrencyId.HasValue) entity.CurrencyId = dto.CurrencyId.Value;
        if (dto.CategoryId.HasValue) entity.CategoryId = dto.CategoryId;
        if (dto.Date.HasValue) entity.Date = dto.Date.Value;
        if (dto.Type.HasValue) entity.Type = dto.Type.Value;
        if (dto.Description != null) entity.Description = dto.Description;
        if (dto.AuthCode != null) entity.AuthCode = dto.AuthCode;

        var newFrom = entity.AccountFrom.HasValue ? await _accountManager.GetByIdAsync(entity.AccountFrom.Value, ct) : null;
        var newTo = entity.AccountTo.HasValue ? await _accountManager.GetByIdAsync(entity.AccountTo.Value, ct) : null;
        if (entity.AccountFrom.HasValue && newFrom == null)
            throw new CustomException("AccountFrom not found", StatusCodes.Status400BadRequest);
        if (entity.AccountTo.HasValue && newTo == null)
            throw new CustomException("AccountTo not found", StatusCodes.Status400BadRequest);

        await _accountManager.ApplyBalanceAsync(entity.Type, entity.Amount, newFrom, newTo, false, ct);

        entity.UnicCode = GenerateUnicCode(entity.Amount, entity.Date, entity.AuthCode);

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

    public async Task<IEnumerable<PreparedTransactionDto>> PrepareAsync(
        IEnumerable<ImportTransactionDto> source,
        CancellationToken ct)
    {
        var result = new List<PreparedTransactionDto>();

        foreach (var item in source)
        {
            var prepared = new PreparedTransactionDto
            {
                Title = item.Title,
                Amount = Math.Abs(item.Amount),
                AccountFrom = item.AccountFrom,
                AccountTo = item.AccountTo,
                Date = item.Date,
                Type = item.Type,
                Description = item.Description,
                AuthCode = item.AuthCode
            };

            prepared.CurrencyId = await _context.Currencies
                .Where(c => c.Code == item.Currency)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);

            var last = await _context.Transactions
                .Where(t => t.Title == item.Title)
                .OrderByDescending(t => t.Date)
                .FirstOrDefaultAsync(ct);

            if (last != null)
            {
                prepared.EventId = last.EventId;
                prepared.BudgetPlanId = last.BudgetPlanId;
                prepared.CategoryId = last.CategoryId;
            }

            var unic = GenerateUnicCode(prepared.Amount ?? 0m, prepared.Date ?? item.Date, prepared.AuthCode);
            var exists = await _context.Transactions
                .AsNoTracking()
                .AnyAsync(t => t.UnicCode == unic, ct);
            if (exists)
                continue;

            result.Add(prepared);
        }

        return result;
    }

    private static string GenerateUnicCode(decimal amount, DateTime date, string? authCode)
    {
        using var sha = SHA256.Create();
        var input = string.IsNullOrWhiteSpace(authCode)
            ? $"{amount}-{date:O}"
            : $"{amount}-{date:O}-{authCode}";
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
