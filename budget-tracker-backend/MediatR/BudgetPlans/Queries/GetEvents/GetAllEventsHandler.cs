using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.BudgetPlans;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetEvents;

public class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, Result<IEnumerable<BudgetPlanDto>>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public GetAllEventsHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanDto>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var plans = await _manager.GetAllAsync(cancellationToken);
        var events = plans.Where(p => p.Type == BudgetPlanType.Event);
        var dtos = _mapper.Map<IEnumerable<BudgetPlanDto>>(events);
        return Result.Ok(dtos);
    }
}
