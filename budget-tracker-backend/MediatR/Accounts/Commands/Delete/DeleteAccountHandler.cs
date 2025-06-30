using budget_tracker_backend.Services.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Delete;

public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Result<bool>>
{
    private readonly IAccountManager _manager;

    public DeleteAccountHandler(IAccountManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}
