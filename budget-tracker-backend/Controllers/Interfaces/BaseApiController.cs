using budget_tracker_backend.Extensions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers.Interfaces;

[ApiController]
[Route("api/[controller]/[action]")]
public class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator => _mediator ??=
        HttpContext.RequestServices.GetService<IMediator>()!;

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is null
                ? this.ApiError(StatusCodes.Status404NotFound, "not_found", "Resource was not found.")
                : Ok(result.Value);
        }

        return this.ApiResultError(result.Reasons);
    }
}
