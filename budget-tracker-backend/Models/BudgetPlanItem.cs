using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class BudgetPlanItem
{
    public int Id { get; set; }
    public int BudgetPlanId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual BudgetPlan? BudgetPlan { get; set; }
    [JsonIgnore]
    public virtual Category? Category { get; set; }
    [JsonIgnore]
    public virtual Currency Currency { get; set; } = null!;
}
