using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.Transactions;

public class CreateTransactionDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public int? BudgetPlanId { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime Date { get; set; }
    public TransactionCategoryType? Type { get; set; }
    public string? Description { get; set; }
    public string? AuthCode { get; set; }
}