using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;

namespace budget_tracker_backend.MediatR.Pages.TransfersByMonth;

public class GetTransfersByMonthHandler
    : IRequestHandler<GetTransfersByMonthQuery, Result<TransfersByMonthDto>>
{
    private readonly IPageManager _manager;
    private readonly IMapper _mapper;

    public GetTransfersByMonthHandler(IPageManager manager, IMapper mapper)
    {
        _manager = manager; _mapper = mapper;
    }

    public async Task<Result<TransfersByMonthDto>> Handle(
        GetTransfersByMonthQuery rq, CancellationToken ct)
    {
        var dto = await _manager.GetTransfersByMonthAsync(rq.Month, rq.Year, ct);
        return Result.Ok(dto);
    }
}
