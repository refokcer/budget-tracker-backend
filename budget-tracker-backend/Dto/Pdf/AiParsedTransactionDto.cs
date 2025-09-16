using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.Pdf;

public class AiParsedTransactionDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public string Date { get; set; } = string.Empty;
    public TransactionCategoryType Type { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? AuthCode { get; set; }
}
