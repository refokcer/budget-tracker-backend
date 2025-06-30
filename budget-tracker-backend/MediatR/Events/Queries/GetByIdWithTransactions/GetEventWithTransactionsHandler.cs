using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Queries.GetByIdWithTransactions;

public class GetEventWithTransactionsHandler : IRequestHandler<GetEventWithTransactionsQuery, Result<EventWithTransactionsDto>>
{
    private readonly IEventManager _manager;
    private readonly IMapper _mapper;

    public GetEventWithTransactionsHandler(IEventManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<EventWithTransactionsDto>> Handle(GetEventWithTransactionsQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await _manager.GetByIdWithTransactionsAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
            return Result.Fail($"Event with Id={request.EventId} not found");

        var dto = _mapper.Map<EventWithTransactionsDto>(eventEntity);
        return Result.Ok(dto);
    }
}