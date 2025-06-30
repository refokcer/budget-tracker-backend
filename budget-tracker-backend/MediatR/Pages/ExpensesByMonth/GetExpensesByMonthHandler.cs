using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.ExpensesByMonth;

public class GetExpensesByMonthHandler
    : IRequestHandler<GetExpensesByMonthQuery, Result<ExpensesByMonthDto>>
{
    private readonly IPageManager _manager;
    private readonly IMapper _mapper;

    public GetExpensesByMonthHandler(IPageManager manager, IMapper mapper)
    {
        _manager = manager; _mapper = mapper;
    }

    public async Task<Result<ExpensesByMonthDto>> Handle(
        GetExpensesByMonthQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetExpensesByMonthAsync(rq.Month, rq.Year, ct);
        return Result.Ok(dto);
    }
}
