using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByBudgetPlan;

public record GetTransactionsByBudgetPlanIdQuery(int BudgetPlanId)
    : IRequest<Result<IEnumerable<TransactionDto>>>;
