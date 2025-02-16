using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Delete;

public record DeleteAccountCommand(int Id) : IRequest<Result<bool>>;
