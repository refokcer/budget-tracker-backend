using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Queries.GetAll;

public class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, Result<IEnumerable<EventDto>>>
{
    private readonly IEventManager _manager;
    private readonly IMapper _mapper;

    public GetAllEventsHandler(IEventManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<EventDto>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<EventDto>>(events);
        return Result.Ok(dtos);
    }
}