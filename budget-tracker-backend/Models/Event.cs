using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int BudgetPlanId { get; set; }
    public string? Description { get; set; }
    public List<Transaction> Transactions { get; set; } = [];

    // Навигационное свойство
    [JsonIgnore]
    public BudgetPlan? BudgetPlan { get; set; }
}
