namespace budget_tracker_backend.Dto.FinancialGoals;

public class CreateFinancialGoalDto
{
    public string Title { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal InitialAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public int? LinkedAccountId { get; set; }
    public string? Description { get; set; }
}
