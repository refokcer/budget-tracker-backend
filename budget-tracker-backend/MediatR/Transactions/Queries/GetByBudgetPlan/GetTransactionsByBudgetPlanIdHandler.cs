using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByBudgetPlan;

public class GetTransactionsByBudgetPlanIdHandler
    : IRequestHandler<GetTransactionsByBudgetPlanIdQuery,
                      Result<IEnumerable<TransactionDto>>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public GetTransactionsByBudgetPlanIdHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(
        GetTransactionsByBudgetPlanIdQuery request,
        CancellationToken ct)
    {
        var transactions = await _manager.GetByBudgetPlanIdAsync(request.BudgetPlanId, ct);

        var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        return Result.Ok(dtos);
    }
}

