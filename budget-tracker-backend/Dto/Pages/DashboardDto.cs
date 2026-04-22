namespace budget_tracker_backend.Dto.Pages;

public class DashboardDto
{
    public List<DashboardAccountDto> Accounts { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public List<DashboardCategoryDto> TopExpenses { get; set; } = new();
    public List<DashboardCategoryDto> TopIncomes { get; set; } = new();
    public DashboardTransactionDto? BiggestTransaction { get; set; }
    public FinancialStabilityDto FinancialStability { get; set; } = new();
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
