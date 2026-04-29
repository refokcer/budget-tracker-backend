using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.Accounts;

public class AccountDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public AccountType Type { get; set; } = AccountType.Other;
    public string? Description { get; set; }
}
