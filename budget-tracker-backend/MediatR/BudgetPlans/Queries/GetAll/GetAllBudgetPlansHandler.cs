using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetAll;

public class GetAllBudgetPlansHandler
    : IRequestHandler<GetAllBudgetPlansQuery, Result<IEnumerable<BudgetPlanDto>>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public GetAllBudgetPlansHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanDto>>> Handle(GetAllBudgetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<BudgetPlanDto>>(plans);
        return Result.Ok(dtos);
    }
}