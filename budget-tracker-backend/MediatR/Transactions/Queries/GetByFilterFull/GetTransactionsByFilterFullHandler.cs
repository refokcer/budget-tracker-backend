using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByFilterFull;

public class GetTransactionsByFilterFullHandler
    : IRequestHandler<GetTransactionsByFilterFullQuery, Result<TransactionsFilteredDto>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetTransactionsByFilterFullHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<TransactionsFilteredDto>> Handle(
        GetTransactionsByFilterFullQuery request,
        CancellationToken ct)
    {
        var list = await _manager.GetFilteredDetailedAsync(
            request.Type,
            request.CategoryId,
            request.StartDate,
            request.EndDate,
            request.BudgetPlanId,
            request.AccountFrom,
            request.AccountTo,
            ct);

        var dtos = _mapper.Map<List<FilteredTxDto>>(list);
        var result = new TransactionsFilteredDto
        {
            Start = request.StartDate ?? DateTime.MinValue,
            End = request.EndDate ?? DateTime.MinValue,
            Transactions = dtos
        };

        return Result.Ok(result);
    }
}
