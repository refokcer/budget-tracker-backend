using System.Text.Json.Serialization;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Models;

public class RecurringPayment : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public TransactionCategoryType Type { get; set; }
    public RecurringPaymentFrequency Frequency { get; set; } = RecurringPaymentFrequency.Monthly;
    public int Interval { get; set; } = 1;
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoCreateTransactions { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public string? Description { get; set; }
    public string UserId { get; set; } = null!;

    [JsonIgnore]
    public virtual Currency? Currency { get; set; }
    [JsonIgnore]
    public virtual Category? Category { get; set; }
    [JsonIgnore]
    public virtual Account? FromAccount { get; set; }
    [JsonIgnore]
    public virtual Account? ToAccount { get; set; }
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}
