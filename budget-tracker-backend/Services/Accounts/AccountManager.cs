namespace budget_tracker_backend.Services.Accounts;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using FluentResults;
using Microsoft.EntityFrameworkCore;

public class AccountManager : IAccountManager
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public AccountManager(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Include(a => a.Currency)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Account> CreateAsync(CreateAccountDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Account>(dto) ?? throw new CustomException("Cannot map CreateAccountDto to Account entity", StatusCodes.Status400BadRequest);

        await _dbContext.Accounts.AddAsync(entity, cancellationToken);
        var saved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            throw new CustomException("Failed to create an account", StatusCodes.Status500InternalServerError);
        }

        return entity;
    }

    public async Task<Account> UpdateAsync(AccountDto dto, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Accounts.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            throw new CustomException("Account not found", StatusCodes.Status404NotFound);
        }

        existing.Title = dto.Title;
        existing.Amount = dto.Amount;
        existing.CurrencyId = dto.CurrencyId;
        existing.Description = dto.Description;

        _dbContext.Accounts.Update(existing);
        var saved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            throw new CustomException("Failed to update account", StatusCodes.Status500InternalServerError);
        }

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { id }, cancellationToken);
        if (account == null)
        {
            throw new CustomException("Account not found", StatusCodes.Status404NotFound);
        }

        _dbContext.Accounts.Remove(account);
        var saved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            throw new CustomException("Failed to delete account", StatusCodes.Status500InternalServerError);
        }

        return true;
    }

    private static void ApplyBalance(
        TransactionCategoryType type,
        decimal amount,
        Account? from,
        Account? to,
        bool reverse)
    {
        var sign = reverse ? -1 : 1;

        switch (type)
        {
            case TransactionCategoryType.Income:
                if (to != null) to.Amount += sign * amount;
                break;

            case TransactionCategoryType.Expense:
                if (from != null) from.Amount -= sign * amount;
                break;

            case TransactionCategoryType.Transaction:
                if (from != null) from.Amount -= sign * amount;
                if (to != null) to.Amount += sign * amount;
                break;
        }
    }

    public Task ApplyBalanceAsync(
        TransactionCategoryType type,
        decimal amount,
        Account? from,
        Account? to,
        bool reverse,
        CancellationToken cancellationToken)
    {
        ApplyBalance(type, amount, from, to, reverse);

        if (from != null)
            _dbContext.Accounts.Update(from);
        if (to != null)
            _dbContext.Accounts.Update(to);

        // ApplyBalance only updates tracked account entities. Persisting these
        // changes should be handled by the caller so that several related
        // updates can be saved in a single transaction.
        return Task.CompletedTask;
    }

    public async Task<Result> HandleTransactionAsync(
        TransactionCategoryType type,
        decimal amount,
        int? fromId,
        int? toId,
        bool reverse,
        CancellationToken cancellationToken)
    {
        Account? from = null;
        if (fromId.HasValue)
        {
            from = await GetByIdAsync(fromId.Value, cancellationToken);
            if (from == null)
                return Result.Fail("AccountFrom not found");
        }

        Account? to = null;
        if (toId.HasValue)
        {
            to = await GetByIdAsync(toId.Value, cancellationToken);
            if (to == null)
                return Result.Fail("AccountTo not found");
        }

        if (!reverse)
        {
            if (type == TransactionCategoryType.Income)
            {
                if (amount <= 0)
                    return Result.Fail("Income must be >0");
            }
            else if (type == TransactionCategoryType.Expense ||
                     type == TransactionCategoryType.Transaction)
            {
                if (from != null && from.Amount - amount < 0)
                    return Result.Fail("Not enough money");
            }
        }

        await ApplyBalanceAsync(type, amount, from, to, reverse, cancellationToken);
        return Result.Ok();
    }
}
