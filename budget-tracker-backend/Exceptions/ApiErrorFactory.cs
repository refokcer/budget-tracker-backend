namespace budget_tracker_backend.Exceptions;

using System.Diagnostics;
using budget_tracker_backend.Dto.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ApiErrorFactory
{
    public static ApiErrorResponseDto Create(
        int status,
        string code,
        string message,
        IEnumerable<string>? errors = null,
        string? traceId = null)
    {
        var errorList = errors?
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct()
            .ToList() ?? new List<string>();

        return new ApiErrorResponseDto
        {
            Status = status,
            Code = code,
            Message = message,
            Errors = errorList,
            TraceId = traceId ?? Activity.Current?.Id
        };
    }

    public static ApiErrorResponseDto FromIdentityErrors(
        int status,
        string code,
        string message,
        IEnumerable<IdentityError> errors)
    {
        return Create(status, code, message, errors.Select(error => error.Description));
    }

    public static ApiErrorResponseDto FromModelState(
        ModelStateDictionary modelState,
        string? traceId = null)
    {
        var errors = modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "Invalid request value."
                : error.ErrorMessage);

        return Create(
            StatusCodes.Status400BadRequest,
            "validation_error",
            "The request is invalid.",
            errors,
            traceId);
    }

    public static ApiErrorResponseDto FromException(Exception exception, string? traceId = null)
    {
        if (exception is CustomException customException)
        {
            return Create(
                customException.StatusCode,
                customException.Code,
                customException.Message,
                customException.Errors,
                traceId);
        }

        if (exception is UnauthorizedAccessException)
        {
            return Create(
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "Authentication is required.",
                traceId: traceId);
        }

        return Create(
            StatusCodes.Status500InternalServerError,
            "server_error",
            "An unexpected error occurred.",
            traceId: traceId);
    }

    public static ApiErrorResponseDto FromStatusCode(int statusCode, string? traceId = null)
    {
        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => Create(statusCode, "unauthorized", "Authentication is required.", traceId: traceId),
            StatusCodes.Status403Forbidden => Create(statusCode, "forbidden", "Access is forbidden.", traceId: traceId),
            StatusCodes.Status404NotFound => Create(statusCode, "not_found", "Resource was not found.", traceId: traceId),
            _ => Create(statusCode, "request_error", "Request failed.", traceId: traceId)
        };
    }

    public static ObjectResult ToObjectResult(ApiErrorResponseDto error)
    {
        return new ObjectResult(error)
        {
            StatusCode = error.Status
        };
    }
}
