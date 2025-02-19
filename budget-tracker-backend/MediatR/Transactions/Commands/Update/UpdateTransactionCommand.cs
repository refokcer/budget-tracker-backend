using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Update;

public record UpdateTransactionCommand(TransactionDto TransactionToUpdate) : IRequest<Result<TransactionDto>>;