namespace budget_tracker_backend.Dto.BudgetPlanItems;

public class BudgetPlanItemDto
{
    public int Id { get; set; }
    public int BudgetPlanId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }
}
