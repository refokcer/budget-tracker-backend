using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class FinancialGoal : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal InitialAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? LinkedAccountId { get; set; }
    public string? Description { get; set; }
    public string UserId { get; set; } = null!;

    [JsonIgnore]
    public Account? LinkedAccount { get; set; }
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}
