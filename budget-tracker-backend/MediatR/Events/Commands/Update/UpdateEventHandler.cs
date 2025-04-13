using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Update;

public class UpdateEventHandler : IRequestHandler<UpdateEventCommand, Result<EventDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateEventHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var dto = request.UpdatedEvent;
        var existing = await _context.Events.FindAsync(new object[] { dto.Id }, cancellationToken);

        if (existing == null)
        {
            return Result.Fail($"Event with Id={dto.Id} not found");
        }

        _mapper.Map(dto, existing);

        _context.Events.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update Event");
        }

        var updatedDto = _mapper.Map<EventDto>(existing);
        return Result.Ok(updatedDto);
    }
}