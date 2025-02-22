using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;

public class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionsHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken token)
    {
        var query = _context.Transactions.AsQueryable();

        // Фильтр по Type, если указано
        if (request.Type.HasValue)
        {
            query = query.Where(t => t.Type == request.Type.Value);
        }

        // Фильтр по дате, если указано
        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.Date >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.Date <= request.EndDate.Value);
        }

        var list = await query
            .AsNoTracking()
            .ToListAsync(token);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(list);
        return Result.Ok(dtos);
    }
}

