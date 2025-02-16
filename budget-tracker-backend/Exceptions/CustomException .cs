using System.Net;

namespace budget_tracker_backend.Exceptions;

public class CustomException : Exception
{
    public int StatusCode { get; }

    public CustomException(string message, int statusCode = (int)HttpStatusCode.BadRequest) : base(message)
    {
        StatusCode = statusCode;
    }
}