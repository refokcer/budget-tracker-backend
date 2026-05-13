namespace budget_tracker_backend.Dto.Pages;

using budget_tracker_backend.Dto.BudgetPlans;

public class BudgetPlanPageDto
{
    public BudgetPlanDto Plan { get; set; } = null!;
    public List<BudgetPlanPageItemDto> Items { get; set; } = new();
    public List<FilteredTxDto> Transactions { get; set; } = new();
    public List<BudgetPlanEventDto> Events { get; set; } = new();
    public MonthEndForecastDto? MonthEndForecast { get; set; }
}

public class MonthEndForecastDto
{
    public DateTime AsOfDate { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int ElapsedDays { get; set; }
    public int RemainingDays { get; set; }
    public decimal ActualSpent { get; set; }
    public decimal ProjectedVariableSpending { get; set; }
    public decimal FutureRecurringSpending { get; set; }
    public decimal ProjectedTotalSpent { get; set; }
    public decimal BudgetLimit { get; set; }
    public decimal ProjectedRemaining { get; set; }
    public string Status { get; set; } = "On track";
}

public class BudgetPlanPageItemDto
{
    public int Id { get; set; }
    public int BudgetPlanId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryTitle { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal ProjectedSpent { get; set; }
    public decimal ProjectedRemaining { get; set; }
    public decimal ProjectedVariableSpending { get; set; }
    public decimal FutureRecurringSpending { get; set; }
    public string? Description { get; set; }
    public bool IsOther { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsEventSummary { get; set; }
}

public class BudgetPlanEventDto
{
    public BudgetPlanDto Plan { get; set; } = null!;
    public List<BudgetPlanPageItemDto> Items { get; set; } = new();
    public List<FilteredTxDto> Transactions { get; set; } = new();
}
