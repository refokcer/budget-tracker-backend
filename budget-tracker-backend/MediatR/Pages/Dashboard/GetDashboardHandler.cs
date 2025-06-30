using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.Dashboard;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IPageManager _manager;
    private readonly IMapper _mapper;

    public GetDashboardHandler(IPageManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetDashboardAsync(ct);
        return Result.Ok(dto);
    }
}
