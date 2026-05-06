namespace budget_tracker_backend.Dto.UserProfile;

public class UserProfileDto
{
    public string Id { get; set; } = null!;
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public List<string> Roles { get; set; } = new();
    public UserProfileStatsDto Stats { get; set; } = new();
}

public class UserProfileStatsDto
{
    public int AccountsCount { get; set; }
    public int CategoriesCount { get; set; }
    public int TransactionsCount { get; set; }
    public int BudgetPlansCount { get; set; }
    public int FinancialGoalsCount { get; set; }
    public decimal TotalBalance { get; set; }
}

public class UpdateUserProfileDto
{
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
