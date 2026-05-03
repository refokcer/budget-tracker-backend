namespace budget_tracker_backend.Dto.FinancialGoals;

public class FinancialGoalForecastDto
{
    public int GoalId { get; set; }
    public string GoalTitle { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal CurrentSavedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressRatio { get; set; }
    public int MonthsRemaining { get; set; }
    public decimal RequiredMonthlyContribution { get; set; }
    public decimal ProjectedMonthlyContribution { get; set; }
    public decimal ContributionGap { get; set; }
    public decimal ForecastedAmountAtTargetDate { get; set; }
    public DateTime? ProjectedCompletionDate { get; set; }
    public decimal ExpectedSavedAmountByNow { get; set; }
    public bool IsAchievable { get; set; }
    public bool IsOffTrack { get; set; }
    public string RiskLevel { get; set; } = null!;
    public List<string> Warnings { get; set; } = new();
    public List<BudgetAdjustmentSuggestionDto> SuggestedBudgetAdjustments { get; set; } = new();
}

public class BudgetAdjustmentSuggestionDto
{
    public int CategoryId { get; set; }
    public string CategoryTitle { get; set; } = null!;
    public decimal AverageMonthlySpending { get; set; }
    public decimal CurrentBudgetLimit { get; set; }
    public decimal RecommendedBudgetLimit { get; set; }
    public decimal SuggestedReduction { get; set; }
}

public class ApplyBudgetAdjustmentsResultDto
{
    public int GoalId { get; set; }
    public int? BudgetPlanId { get; set; }
    public int AppliedAdjustmentsCount { get; set; }
    public List<BudgetAdjustmentSuggestionDto> AppliedAdjustments { get; set; } = new();
    public FinancialGoalForecastDto Forecast { get; set; } = new();
}
