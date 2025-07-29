using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.Transactions;

/// <summary>
/// DTO used to import raw transaction data. Currency is specified as code string.
/// Other relational identifiers may be missing.
/// </summary>
public class ImportTransactionDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public DateTime Date { get; set; }
    public TransactionCategoryType Type { get; set; }
    public string? Description { get; set; }
    public string? AuthCode { get; set; }
}
