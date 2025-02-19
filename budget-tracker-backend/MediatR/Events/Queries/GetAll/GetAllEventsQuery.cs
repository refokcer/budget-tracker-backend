using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Queries.GetAll;

public record GetAllEventsQuery : IRequest<Result<IEnumerable<EventDto>>>;