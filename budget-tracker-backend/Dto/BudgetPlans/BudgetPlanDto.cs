namespace budget_tracker_backend.Dto.BudgetPlans;

public class BudgetPlanDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Type { get; set; }
    public string? Description { get; set; }
}
