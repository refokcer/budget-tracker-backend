using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetAll;

public record GetAllTransactionsQuery : IRequest<Result<IEnumerable<TransactionDto>>>;