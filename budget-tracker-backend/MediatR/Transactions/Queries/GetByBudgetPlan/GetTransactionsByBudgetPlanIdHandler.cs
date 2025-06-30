using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByBudgetPlan;

public class GetTransactionsByBudgetPlanIdHandler
    : IRequestHandler<GetTransactionsByBudgetPlanIdQuery,
                      Result<IEnumerable<TransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionsByBudgetPlanIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(
        GetTransactionsByBudgetPlanIdQuery request,
        CancellationToken ct)
    {
        var transactions = await _context.Transactions
            .Where(t => t.BudgetPlanId == request.BudgetPlanId)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}

