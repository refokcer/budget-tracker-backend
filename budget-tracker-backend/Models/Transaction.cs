namespace budget_tracker_backend.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int EventId { get; set; }
    public int CurrencyId { get; set; }
    public int CategoryId { get; set; }
    public DateTime Date { get; set; }
    public int? AccountFrom { get; set; }
    public int? AccountTo {  get; set; }
    public TransactionCategoryType Type { get; set; }
    public string? Description { get; set; }

    // Навигационные свойства
    public virtual Category Category { get; set; } = new Category();
    public virtual Currency Currency { get; set; } = new Currency();
    public virtual Event Event { get; set; } = new Event();
    public virtual Account Account { get; set; } = new Account();
}
