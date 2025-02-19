using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Commands.Create;

public record CreateEventCommand(CreateEventDto NewEvent) : IRequest<Result<EventDto>>;