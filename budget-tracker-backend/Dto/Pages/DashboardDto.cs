namespace budget_tracker_backend.Dto.Pages;

public class DashboardDto
{
    public List<DashboardAccountDto> Accounts { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public List<DashboardCategoryDto> TopExpenses { get; set; } = new();
    public List<DashboardCategoryDto> TopIncomes { get; set; } = new();
    public DashboardTransactionDto? BiggestTransaction { get; set; }
    public FinancialStabilityDto FinancialStability { get; set; } = new();
    public BehavioralScoreDto BehavioralScore { get; set; } = new();
    public List<DashboardFinancialGoalDto> FinancialGoals { get; set; } = new();
}

public class DashboardAccountDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
}

public class DashboardCategoryDto
{
    public string CategoryTitle { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Percent { get; set; } = null!;
    public string? Color { get; set; }
}

public class DashboardTransactionDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public DateTime Date { get; set; }
}

public class FinancialStabilityDto
{
    public int Index { get; set; }
    public string Level { get; set; } = null!;
    public FinancialStabilityMetricsDto Metrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class FinancialStabilityMetricsDto
{
    public decimal MandatoryExpensesShare { get; set; }
    public decimal SavingsShare { get; set; }
    public decimal EmergencyFundMonths { get; set; }
    public decimal OverspendingFrequency { get; set; }
    public decimal IncomeStability { get; set; }
    public decimal GoalAchievementIndex { get; set; }
    public decimal AverageMonthlyIncome { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
}

public class BehavioralScoreDto
{
    public int Score { get; set; }
    public string Level { get; set; } = null!;
    public BehavioralScoreMetricsDto Metrics { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class BehavioralScoreMetricsDto
{
    public decimal LimitAdherence { get; set; }
    public decimal ImpulseControl { get; set; }
    public decimal SavingsRegularity { get; set; }
    public decimal WarningResponse { get; set; }
    public int PlannedCategoriesChecked { get; set; }
    public int OverspentCategories { get; set; }
    public int ImpulsiveTransactions { get; set; }
    public decimal ImpulsiveAmountShare { get; set; }
    public int SavingMonths { get; set; }
    public int ActiveIncomeMonths { get; set; }
    public int WarningEvents { get; set; }
    public int ResolvedWarningEvents { get; set; }
}

public class DashboardFinancialGoalDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal CurrentSavedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressRatio { get; set; }
    public decimal RequiredMonthlyContribution { get; set; }
    public bool IsAchievable { get; set; }
    public bool IsOffTrack { get; set; }
    public string RiskLevel { get; set; } = null!;
    public DateTime TargetDate { get; set; }
}
