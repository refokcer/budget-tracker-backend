using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Delete;

public class DeleteEventHandler : IRequestHandler<DeleteEventCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteEventHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"Event with Id={request.Id} not found");
        }

        _context.Events.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        return saved ? Result.Ok(true) : Result.Fail("Failed to delete Event");
    }
}