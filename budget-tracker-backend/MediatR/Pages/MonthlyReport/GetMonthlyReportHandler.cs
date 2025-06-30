using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.MonthlyReport;

public class GetMonthlyReportHandler : IRequestHandler<GetMonthlyReportQuery, Result<MonthlyReportDto>>
{
    private readonly IPageManager _manager;

    public GetMonthlyReportHandler(IPageManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<MonthlyReportDto>> Handle(GetMonthlyReportQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetMonthlyReportAsync(rq.Month, rq.Year, ct);
        return Result.Ok(dto);
    }
}
