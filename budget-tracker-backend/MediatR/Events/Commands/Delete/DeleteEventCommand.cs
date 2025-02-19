using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Events.Commands.Delete;

public record DeleteEventCommand(int Id) : IRequest<Result<bool>>;