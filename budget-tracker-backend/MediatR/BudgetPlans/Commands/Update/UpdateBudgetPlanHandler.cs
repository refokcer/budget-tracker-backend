using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Update;

public class UpdateBudgetPlanHandler : IRequestHandler<UpdateBudgetPlanCommand, Result<BudgetPlanDto>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public UpdateBudgetPlanHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(UpdateBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.UpdateAsync(request.PlanToUpdate, cancellationToken);
        var updatedDto = _mapper.Map<BudgetPlanDto>(entity);
        return Result.Ok(updatedDto);
    }
}