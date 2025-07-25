using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByFilterFull;

public record GetTransactionsByFilterFullQuery(
    TransactionCategoryType? Type,
    int? CategoryId,
    DateTime? StartDate,
    DateTime? EndDate,
    int? BudgetPlanId,
    int? AccountFrom,
    int? AccountTo
) : IRequest<Result<TransactionsFilteredDto>>;
