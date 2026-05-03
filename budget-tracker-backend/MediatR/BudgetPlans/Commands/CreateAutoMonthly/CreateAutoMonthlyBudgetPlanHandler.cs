using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.CreateAutoMonthly;

public class CreateAutoMonthlyBudgetPlanHandler
    : IRequestHandler<CreateAutoMonthlyBudgetPlanCommand, Result<AutoBudgetPlanResultDto>>
{
    private readonly IBudgetPlanManager _manager;

    public CreateAutoMonthlyBudgetPlanHandler(IBudgetPlanManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<AutoBudgetPlanResultDto>> Handle(
        CreateAutoMonthlyBudgetPlanCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _manager.CreateAutoMonthlyPlanAsync(request.Request, cancellationToken);
        return Result.Ok(result);
    }
}
