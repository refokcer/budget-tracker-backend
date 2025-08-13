using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Account : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }
    public string UserId { get; set; } = null!;

    // Navigation properties
    [JsonIgnore]
    public virtual Currency? Currency { get; set; }
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}
