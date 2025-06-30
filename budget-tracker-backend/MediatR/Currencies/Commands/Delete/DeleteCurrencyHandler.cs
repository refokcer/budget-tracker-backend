using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Delete;

public class DeleteCurrencyHandler : IRequestHandler<DeleteCurrencyCommand, Result<bool>>
{
    private readonly ICurrencyManager _manager;

    public DeleteCurrencyHandler(ICurrencyManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteCurrencyCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}