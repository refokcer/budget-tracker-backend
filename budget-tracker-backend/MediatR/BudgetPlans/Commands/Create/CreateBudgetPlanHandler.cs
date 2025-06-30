using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Create;

public class CreateBudgetPlanHandler : IRequestHandler<CreateBudgetPlanCommand, Result<BudgetPlanDto>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public CreateBudgetPlanHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(CreateBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.CreateAsync(request.NewPlan, cancellationToken);
        var resultDto = _mapper.Map<BudgetPlanDto>(entity);
        return Result.Ok(resultDto);
    }
}
