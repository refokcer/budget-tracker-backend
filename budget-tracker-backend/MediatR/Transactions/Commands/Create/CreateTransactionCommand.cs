using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Create;

public record CreateTransactionCommand(CreateTransactionDto NewTransaction) : IRequest<Result<TransactionDto>>;