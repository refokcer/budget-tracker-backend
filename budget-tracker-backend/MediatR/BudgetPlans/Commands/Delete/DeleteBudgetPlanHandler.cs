using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Delete;

public class DeleteBudgetPlanHandler : IRequestHandler<DeleteBudgetPlanCommand, Result<bool>>
{
    private readonly IBudgetPlanManager _manager;

    public DeleteBudgetPlanHandler(IBudgetPlanManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}