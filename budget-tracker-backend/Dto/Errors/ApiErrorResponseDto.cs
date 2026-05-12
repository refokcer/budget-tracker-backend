namespace budget_tracker_backend.Dto.Errors;

public class ApiErrorResponseDto
{
    public int Status { get; set; }
    public string Code { get; set; } = "error";
    public string Message { get; set; } = "An error occurred.";
    public List<string> Errors { get; set; } = new();
    public string? TraceId { get; set; }
}
