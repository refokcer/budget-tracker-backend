using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Create;

public class CreateEventHandler : IRequestHandler<CreateEventCommand, Result<EventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateEventHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Event>(request.NewEvent);
        if (entity == null)
        {
            return Result.Fail("Cannot map CreateEventDto to Event entity");
        }

        _context.Events.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to create Event in database");
        }

        var eventDto = _mapper.Map<EventDto>(entity);
        return Result.Ok(eventDto);
    }
}