using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Queries.GetById;

public class GetEventByIdHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly IEventManager _manager;
    private readonly IMapper _mapper;

    public GetEventByIdHandler(IEventManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result.Fail($"Event with Id={request.Id} not found");

        var dto = _mapper.Map<EventDto>(entity);
        return Result.Ok(dto);
    }
}