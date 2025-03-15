using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetByEvent;

public record GetAllTransactionsByEventQuery(int EventId) : IRequest<Result<IEnumerable<TransactionDto>>>;