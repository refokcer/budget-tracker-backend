using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByEvent;

public class GetAllTransactionsByEventHandler : IRequestHandler<GetAllTransactionsByEventQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetAllTransactionsByEventHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetAllTransactionsByEventQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _manager.GetByEventIdAsync(request.EventId, cancellationToken);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}