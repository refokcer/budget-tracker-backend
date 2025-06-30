using FluentResults;
using MediatR;
using budget_tracker_backend.Services.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Delete;

public class DeleteBudgetPlanItemHandler : IRequestHandler<DeleteBudgetPlanItemCommand, Result<bool>>
{
    private readonly IBudgetPlanItemManager _manager;

    public DeleteBudgetPlanItemHandler(IBudgetPlanItemManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}