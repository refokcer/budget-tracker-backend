namespace budget_tracker_backend.Dto.Auth;

public class RefreshRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
