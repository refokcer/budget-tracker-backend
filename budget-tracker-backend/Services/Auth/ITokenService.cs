using System.Security.Claims;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Services.Auth;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken CreateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
