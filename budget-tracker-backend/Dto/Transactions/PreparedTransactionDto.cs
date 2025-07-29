using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.Transactions;

/// <summary>
/// Transaction DTO with fields filled from existing data.
/// </summary>
public class PreparedTransactionDto
{
    public string? Title { get; set; }
    public decimal? Amount { get; set; }
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public int? EventId { get; set; }
    public int? BudgetPlanId { get; set; }
    public int? CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? Date { get; set; }
    public TransactionCategoryType? Type { get; set; }
    public string? Description { get; set; }
    public string? AuthCode { get; set; }
}
