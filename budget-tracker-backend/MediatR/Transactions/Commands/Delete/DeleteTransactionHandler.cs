using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Helpers;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Dto.Accounts;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Delete;

public class DeleteTransactionHandler
    : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IAccountManager _accountManager;

    public DeleteTransactionHandler(IApplicationDbContext ctx, IAccountManager accountManager)
    {
        _ctx = ctx;
        _accountManager = accountManager;
    }

    public async Task<Result<bool>> Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var tr = await _ctx.Transactions
                           .AsNoTracking()
                           .FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (tr is null)
            return Result.Fail($"Transaction {request.Id} not found");

        // Загружаем связанные аккаунты (если есть)
        var fromAcc = tr.AccountFrom.HasValue
            ? await _accountManager.GetByIdAsync(tr.AccountFrom.Value, ct)
            : null;

        var toAcc = tr.AccountTo.HasValue
            ? await _accountManager.GetByIdAsync(tr.AccountTo.Value, ct)
            : null;

        // Откатить влияние транзакции
        AccountBalanceHelper.Apply(tr.Type, tr.Amount, fromAcc, toAcc, reverse: true);
        if (fromAcc != null)
            await _accountManager.UpdateAsync(new AccountDto
            {
                Id = fromAcc.Id,
                Title = fromAcc.Title,
                Amount = fromAcc.Amount,
                CurrencyId = fromAcc.CurrencyId,
                Description = fromAcc.Description
            }, ct);
        if (toAcc != null)
            await _accountManager.UpdateAsync(new AccountDto
            {
                Id = toAcc.Id,
                Title = toAcc.Title,
                Amount = toAcc.Amount,
                CurrencyId = toAcc.CurrencyId,
                Description = toAcc.Description
            }, ct);

        _ctx.Transactions.Remove(tr);
        var saved = await _ctx.SaveChangesAsync(ct) > 0;

        return saved
            ? Result.Ok(true)
            : Result.Fail("Failed to delete Transaction");
    }
}
