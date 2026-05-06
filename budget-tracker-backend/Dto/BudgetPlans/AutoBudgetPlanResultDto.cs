namespace budget_tracker_backend.Dto.BudgetPlans;

public class AutoBudgetPlanResultDto
{
    public BudgetPlanDto Plan { get; set; } = null!;
    public int SourcePlanId { get; set; }
    public string SourcePlanTitle { get; set; } = null!;
    public DateTime SourceStartDate { get; set; }
    public DateTime SourceEndDate { get; set; }
    public decimal PreviousTotal { get; set; }
    public decimal NewTotal { get; set; }
    public decimal AverageMonthlyIncome { get; set; }
    public decimal GoalReserve { get; set; }
    public decimal ExpenseEnvelope { get; set; }
    public decimal IncomeEnvelopeMultiplier { get; set; } = 1m;
    public decimal FinancialStateIndex { get; set; }
    public decimal PreviousFinancialStateIndex { get; set; }
    public decimal FinancialStateTrend { get; set; }
    public List<AutoBudgetPlanItemResultDto> Items { get; set; } = new();
}

public class AutoBudgetPlanItemResultDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryTitle { get; set; } = null!;
    public decimal PreviousLimit { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal OverspentAmount { get; set; }
    public decimal CarryAdjustment { get; set; }
    public decimal HistoryAverageAmount { get; set; }
    public string Priority { get; set; } = null!;
    public decimal SeasonalityMultiplier { get; set; }
    public decimal FinancialStateMultiplier { get; set; } = 1m;
    public decimal IncomeEnvelopeMultiplier { get; set; } = 1m;
    public decimal RecommendedAmount { get; set; }
    public int CurrencyId { get; set; }
    public string? Description { get; set; }
}
