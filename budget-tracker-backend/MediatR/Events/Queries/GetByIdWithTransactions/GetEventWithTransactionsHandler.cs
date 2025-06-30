using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Queries.GetByIdWithTransactions;

public class GetEventWithTransactionsHandler : IRequestHandler<GetEventWithTransactionsQuery, Result<EventWithTransactionsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventWithTransactionsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventWithTransactionsDto>> Handle(GetEventWithTransactionsQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Transactions) 
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity == null)
        {
            return Result.Fail($"Event with Id={request.EventId} not found");
        }

        var dto = _mapper.Map<EventWithTransactionsDto>(eventEntity);
        return Result.Ok(dto);
    }
}