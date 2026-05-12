using System.Net;

namespace budget_tracker_backend.Exceptions;

public class CustomException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public IReadOnlyCollection<string> Errors { get; }

    public CustomException(
        string message,
        int statusCode = (int)HttpStatusCode.BadRequest,
        string code = "request_error",
        IEnumerable<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray()
            ?? Array.Empty<string>();
    }
}
