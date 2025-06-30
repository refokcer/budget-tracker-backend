using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Helpers;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Delete;

public class DeleteTransactionHandler
    : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _ctx;

    public DeleteTransactionHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<Result<bool>> Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var tr = await _ctx.Transactions
                           .AsNoTracking()
                           .FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (tr is null)
            return Result.Fail($"Transaction {request.Id} not found");

        // Загружаем связанные аккаунты (если есть)
        var fromAcc = tr.AccountFrom.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountFrom.Value }, ct)
            : null;

        var toAcc = tr.AccountTo.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountTo.Value }, ct)
            : null;

        // Откатить влияние транзакции
        AccountBalanceHelper.Apply(tr.Type, tr.Amount, fromAcc, toAcc, reverse: true);

        _ctx.Transactions.Remove(tr);
        var saved = await _ctx.SaveChangesAsync(ct) > 0;

        return saved
            ? Result.Ok(true)
            : Result.Fail("Failed to delete Transaction");
    }
}
