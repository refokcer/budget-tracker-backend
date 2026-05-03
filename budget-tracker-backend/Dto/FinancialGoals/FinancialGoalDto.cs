namespace budget_tracker_backend.Dto.FinancialGoals;

public class FinancialGoalDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal InitialAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? LinkedAccountId { get; set; }
    public string? Description { get; set; }
}
