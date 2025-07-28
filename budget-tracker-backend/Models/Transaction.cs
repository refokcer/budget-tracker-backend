using budget_tracker_backend.Models.Enums;
using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? EventId { get; set; }
    public int? BudgetPlanId { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime Date { get; set; }
    public string UnicCode { get; set; } = null!;

    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; } 

    public TransactionCategoryType Type { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual BudgetPlan? BudgetPlan { get; set; }
    [JsonIgnore]
    public virtual Category? Category { get; set; }
    [JsonIgnore]
    public virtual Currency? Currency { get; set; }
    [JsonIgnore]
    public virtual Event? Event { get; set; }
    [JsonIgnore]
    public virtual Account? FromAccount { get; set; }
    [JsonIgnore]
    public virtual Account? ToAccount { get; set; }
}
