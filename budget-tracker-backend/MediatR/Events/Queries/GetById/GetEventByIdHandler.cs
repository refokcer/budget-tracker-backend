using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Queries.GetById;

public class GetEventByIdHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventByIdHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"Event with Id={request.Id} not found");
        }

        var dto = _mapper.Map<EventDto>(entity);
        return Result.Ok(dto);
    }
}