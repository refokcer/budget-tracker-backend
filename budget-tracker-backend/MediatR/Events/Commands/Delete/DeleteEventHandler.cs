using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Events.Commands.Delete;

public class DeleteEventHandler : IRequestHandler<DeleteEventCommand, Result<bool>>
{
    private readonly IEventManager _manager;

    public DeleteEventHandler(IEventManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}