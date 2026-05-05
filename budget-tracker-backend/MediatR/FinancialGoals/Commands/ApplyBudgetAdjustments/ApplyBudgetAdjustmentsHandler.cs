using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.ApplyBudgetAdjustments;

public class ApplyBudgetAdjustmentsHandler : IRequestHandler<ApplyBudgetAdjustmentsCommand, Result<ApplyBudgetAdjustmentsResultDto>>
{
    private readonly IFinancialGoalManager _manager;

    public ApplyBudgetAdjustmentsHandler(IFinancialGoalManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<ApplyBudgetAdjustmentsResultDto>> Handle(ApplyBudgetAdjustmentsCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.ApplyBudgetAdjustmentsAsync(request.GoalId, request.Dto, cancellationToken);
        return Result.Ok(result);
    }
}
