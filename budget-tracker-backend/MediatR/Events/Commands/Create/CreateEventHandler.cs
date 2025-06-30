using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Create;

public class CreateEventHandler : IRequestHandler<CreateEventCommand, Result<EventDto>>
{
    private readonly IEventManager _manager;
    private readonly IMapper _mapper;

    public CreateEventHandler(IEventManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.CreateAsync(request.NewEvent, cancellationToken);
        var eventDto = _mapper.Map<EventDto>(entity);
        return Result.Ok(eventDto);
    }
}