using budget_tracker_backend.Dto.Transactions;

namespace budget_tracker_backend.Dto.Events;

public class EventWithTransactionsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int BudgetPlanId { get; set; }
    public string? Description { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();
}
