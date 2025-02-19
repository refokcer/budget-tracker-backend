using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Queries.GetById;

public record GetEventByIdQuery(int Id) : IRequest<Result<EventDto>>;