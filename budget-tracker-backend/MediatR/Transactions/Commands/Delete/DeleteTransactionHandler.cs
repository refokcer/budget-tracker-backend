using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Delete;

public class DeleteTransactionHandler
    : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly ITransactionManager _manager;

    public DeleteTransactionHandler(ITransactionManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var result = await _manager.DeleteAsync(request.Id, ct);
        return Result.Ok(result);
    }
}
