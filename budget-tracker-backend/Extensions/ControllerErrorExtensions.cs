namespace budget_tracker_backend.Extensions;

using budget_tracker_backend.Exceptions;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public static class ControllerErrorExtensions
{
    public static ObjectResult ApiError(
        this ControllerBase controller,
        int status,
        string code,
        string message,
        IEnumerable<string>? errors = null)
    {
        var response = ApiErrorFactory.Create(
            status,
            code,
            message,
            errors,
            controller.HttpContext.TraceIdentifier);

        return ApiErrorFactory.ToObjectResult(response);
    }

    public static ObjectResult ApiValidationError(
        this ControllerBase controller,
        string message,
        IEnumerable<IdentityError> errors)
    {
        var response = ApiErrorFactory.FromIdentityErrors(
            StatusCodes.Status400BadRequest,
            "validation_error",
            message,
            errors);

        response.TraceId = controller.HttpContext.TraceIdentifier;
        return ApiErrorFactory.ToObjectResult(response);
    }

    public static ObjectResult ApiValidationError(
        this ControllerBase controller,
        string message,
        IEnumerable<string> errors)
    {
        return controller.ApiError(
            StatusCodes.Status400BadRequest,
            "validation_error",
            message,
            errors);
    }

    public static ObjectResult ApiResultError(
        this ControllerBase controller,
        IEnumerable<IReason> reasons)
    {
        return controller.ApiError(
            StatusCodes.Status400BadRequest,
            "request_error",
            "Request failed.",
            reasons.Select(reason => reason.Message));
    }
}
