using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.Delete;

public class DeleteFinancialGoalHandler : IRequestHandler<DeleteFinancialGoalCommand, Result<bool>>
{
    private readonly IFinancialGoalManager _manager;

    public DeleteFinancialGoalHandler(IFinancialGoalManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteFinancialGoalCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(deleted);
    }
}
