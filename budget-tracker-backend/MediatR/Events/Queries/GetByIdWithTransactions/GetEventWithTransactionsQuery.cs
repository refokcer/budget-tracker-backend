using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Queries.GetByIdWithTransactions;

public record GetEventWithTransactionsQuery(int EventId)
    : IRequest<Result<EventWithTransactionsDto>>;
