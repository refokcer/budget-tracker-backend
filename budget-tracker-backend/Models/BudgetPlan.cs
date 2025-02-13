using budget_tracker_backend.Models.Enums;
using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class BudgetPlan
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetPlanType Type { get; set; }
    public string? Description { get; set; }

    // Навигационные свойства
    [JsonIgnore]
    public virtual Category? Category { get; set; }
}
