using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetById;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetTransactionByIdHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result.Fail($"Transaction with Id={request.Id} not found");

        var dto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(dto);
    }
}