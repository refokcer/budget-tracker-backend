using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Extensions;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;

public class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken token)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .WhereIf(request.Type.HasValue, t => t.Type == request.Type!.Value)
            .WhereIf(request.StartDate.HasValue, t => t.Date >= request.StartDate!.Value)
            .WhereIf(request.EndDate.HasValue, t => t.Date <= request.EndDate!.Value)
            .WhereIf(request.EventId.HasValue, t => t.EventId == request.EventId!.Value);

        var list = await query.ToListAsync(token);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(list);
        return Result.Ok(dtos);
    }
}

