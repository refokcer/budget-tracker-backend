namespace budget_tracker_backend.Dto.Accounts;

public class CreateAccountDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }
}
