namespace budget_tracker_backend.Dto.Events;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int BudgetPlanId { get; set; }
    public string? Description { get; set; }
}