using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace budget_tracker_backend.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

