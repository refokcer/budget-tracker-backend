using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Delete;

public record DeleteTransactionCommand(int Id) : IRequest<Result<bool>>;