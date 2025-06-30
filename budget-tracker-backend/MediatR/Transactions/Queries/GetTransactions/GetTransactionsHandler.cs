using AutoMapper;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Extensions;
using budget_tracker_backend.Services.Transactions;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;

public class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetTransactionsHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken token)
    {
        var list = await _manager.GetFilteredAsync(
            request.Type,
            request.StartDate,
            request.EndDate,
            request.EventId,
            token);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(list);
        return Result.Ok(dtos);
    }
}

