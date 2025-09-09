using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(
    TransactionCategoryType? Type = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<Result<IEnumerable<TransactionDto>>>;
