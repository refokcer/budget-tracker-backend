using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByEvent;

public class GetAllTransactionsByEventHandler : IRequestHandler<GetAllTransactionsByEventQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllTransactionsByEventHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetAllTransactionsByEventQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _context.Transactions
            .Where(t => t.EventId == request.EventId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}