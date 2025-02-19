using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Commands.Update;

public record UpdateEventCommand(EventDto UpdatedEvent) : IRequest<Result<EventDto>>;