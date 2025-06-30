using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.IncomesByMonth;

public class GetIncomesByMonthHandler
    : IRequestHandler<GetIncomesByMonthQuery, Result<IncomesByMonthDto>>
{
    private readonly IPageManager _manager;
    private readonly IMapper _mapper;

    public GetIncomesByMonthHandler(IPageManager manager, IMapper mapper)
    {
        _manager = manager; _mapper = mapper;
    }

    public async Task<Result<IncomesByMonthDto>> Handle(
        GetIncomesByMonthQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetIncomesByMonthAsync(rq.Month, rq.Year, ct);
        return Result.Ok(dto);
    }
}
