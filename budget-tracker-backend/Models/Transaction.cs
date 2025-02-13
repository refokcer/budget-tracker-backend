using budget_tracker_backend.Models.Enums;
using System.Text.Json.Serialization;

namespace budget_tracker_backend.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? EventId { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime Date { get; set; }

    public int? AccountFrom { get; set; } // Ссылка на счет, с которого списаны деньги
    public int? AccountTo { get; set; }   // Ссылка на счет, на который зачислены деньги

    public TransactionCategoryType Type { get; set; }
    public string? Description { get; set; }

    // Навигационные свойства
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
