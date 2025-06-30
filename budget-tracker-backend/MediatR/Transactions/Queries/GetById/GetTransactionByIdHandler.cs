using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetById;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionByIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Transactions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"Transaction with Id={request.Id} not found");
        }

        var dto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(dto);
    }
}