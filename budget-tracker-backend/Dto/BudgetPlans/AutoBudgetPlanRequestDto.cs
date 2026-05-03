namespace budget_tracker_backend.Dto.BudgetPlans;

public class AutoBudgetPlanRequestDto
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Title { get; set; }
    public bool ReplaceExisting { get; set; }
    public bool ApplySeasonality { get; set; } = true;
    public decimal OverspendCarryRate { get; set; } = 0.5m;
    public decimal UnderspendCarryRate { get; set; } = 0.25m;
    public List<AutoBudgetPlanSeasonalityOverrideDto> SeasonalityOverrides { get; set; } = new();
}

public class AutoBudgetPlanSeasonalityOverrideDto
{
    public int CategoryId { get; set; }
    public decimal Multiplier { get; set; }
}
