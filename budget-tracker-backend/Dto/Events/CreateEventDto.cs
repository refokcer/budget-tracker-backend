namespace budget_tracker_backend.Dto.Events;

public class CreateEventDto
{
    public string Title { get; set; } = null!;
    public int BudgetPlanId { get; set; }
    public string? Description { get; set; }
}