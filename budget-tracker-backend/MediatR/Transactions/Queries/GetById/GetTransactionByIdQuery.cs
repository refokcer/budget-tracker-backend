using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Queries.GetById;

public record GetTransactionByIdQuery(int Id) : IRequest<Result<TransactionDto>>;