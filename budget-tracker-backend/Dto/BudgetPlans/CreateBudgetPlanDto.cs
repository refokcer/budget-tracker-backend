using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.BudgetPlans;

public class CreateBudgetPlanDto
{
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetPlanType Type { get; set; }
    public string? Description { get; set; }
}
