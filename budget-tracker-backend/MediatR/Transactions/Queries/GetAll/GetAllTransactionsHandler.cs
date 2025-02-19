using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetAll;

public class GetAllTransactionsHandler : IRequestHandler<GetAllTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllTransactionsHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _context.Transactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}