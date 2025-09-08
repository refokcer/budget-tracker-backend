namespace budget_tracker_backend.Dto.Pages;

using budget_tracker_backend.Dto.BudgetPlans;

public class BudgetPlanPageDto
{
    public BudgetPlanDto Plan { get; set; } = null!;
    public List<BudgetPlanPageItemDto> Items { get; set; } = new();
    public List<FilteredTxDto> Transactions { get; set; } = new();
}

public class BudgetPlanPageItemDto
{
    public int Id { get; set; }
    public string CategoryTitle { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public string? Description { get; set; }
}
