using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.BudgetPlanPage;

public class GetBudgetPlanPageHandler : IRequestHandler<GetBudgetPlanPageQuery, Result<BudgetPlanPageDto>>
{
    private readonly IPageManager _manager;
    private readonly IMapper _mapper;

    public GetBudgetPlanPageHandler(IPageManager manager, IMapper mapper)
    {
        _manager = manager; _mapper = mapper;
    }

    public async Task<Result<BudgetPlanPageDto>> Handle(GetBudgetPlanPageQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetBudgetPlanPageAsync(rq.PlanId, rq.IncludeEvents, ct);
        return Result.Ok(dto);
    }
}
