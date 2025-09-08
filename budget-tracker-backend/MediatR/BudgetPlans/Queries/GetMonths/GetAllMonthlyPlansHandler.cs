using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.BudgetPlans;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetMonths;

public class GetAllMonthlyPlansHandler : IRequestHandler<GetAllMonthlyPlansQuery, Result<IEnumerable<BudgetPlanDto>>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public GetAllMonthlyPlansHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanDto>>> Handle(GetAllMonthlyPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _manager.GetAllAsync(cancellationToken);
        var months = plans.Where(p => p.Type == BudgetPlanType.Monthly);
        var dtos = _mapper.Map<IEnumerable<BudgetPlanDto>>(months);
        return Result.Ok(dtos);
    }
}
