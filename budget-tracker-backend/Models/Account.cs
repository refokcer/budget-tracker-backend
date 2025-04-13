using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Account
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual Currency? Currency { get; set; }
}
