using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.EventPage;

public class GetEventPageHandler : IRequestHandler<GetEventPageQuery, Result<BudgetPlanEventDto>>
{
    private readonly IPageManager _manager;

    public GetEventPageHandler(IPageManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<BudgetPlanEventDto>> Handle(GetEventPageQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetEventPageAsync(rq.EventId, ct);
        return Result.Ok(dto);
    }
}
