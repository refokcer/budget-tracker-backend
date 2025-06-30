using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Update;

public class UpdateEventHandler : IRequestHandler<UpdateEventCommand, Result<EventDto>>
{
    private readonly IEventManager _manager;
    private readonly IMapper _mapper;

    public UpdateEventHandler(IEventManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.UpdateAsync(request.UpdatedEvent, cancellationToken);
        var updatedDto = _mapper.Map<EventDto>(entity);
        return Result.Ok(updatedDto);
    }
}