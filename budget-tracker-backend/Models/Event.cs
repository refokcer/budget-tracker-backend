using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Event : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int BudgetPlanId { get; set; }
    public string? Description { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
    public string UserId { get; set; } = null!;

    // Navigation properties
    [JsonIgnore]
    public BudgetPlan? BudgetPlan { get; set; }
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}
