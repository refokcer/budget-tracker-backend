using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models.Enums;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(
    TransactionCategoryType? Type,
    DateTime? StartDate,
    DateTime? EndDate
) : IRequest<Result<IEnumerable<TransactionDto>>>;

