using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Delete;

public record DeleteCurrencyCommand(int Id) : IRequest<Result<bool>>;