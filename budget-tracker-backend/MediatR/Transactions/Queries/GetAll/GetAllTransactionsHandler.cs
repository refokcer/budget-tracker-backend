using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetAll;

public class GetAllTransactionsHandler : IRequestHandler<GetAllTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetAllTransactionsHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}