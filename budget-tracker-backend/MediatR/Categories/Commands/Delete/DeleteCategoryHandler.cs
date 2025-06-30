using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Commands.Delete;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly ICategoryManager _manager;

    public DeleteCategoryHandler(ICategoryManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(request.Id, cancellationToken);
        return Result.Ok(result);
    }
}
