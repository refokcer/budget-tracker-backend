using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;

namespace budget_tracker_backend.MediatR.Events.Queries.GetAll;

public class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, Result<IEnumerable<EventDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllEventsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<EventDto>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _context.Events
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<EventDto>>(events);
        return Result.Ok(dtos);
    }
}