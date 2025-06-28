namespace budget_tracker_backend.Dto.Pages;

public class MonthlyReportDto
{
    public decimal TotalExp { get; set; }
    public decimal TotalInc { get; set; }
    public decimal Balance { get; set; }
    public string DefaultCurrency { get; set; } = null!;
    public List<LabelAmountPercentDto> TopExpenseCategories { get; set; } = new();
    public List<LabelAmountPercentDto> TopIncomeCategories { get; set; } = new();
    public List<LabelValueDto> ExpensesByCategory { get; set; } = new();
    public List<LabelValueDto> IncomesByCategory { get; set; } = new();
    public List<LabelValueDto> ExpensesByAccount { get; set; } = new();
    public MonthlyReportTxDto? TopExpenseTransaction { get; set; }
}

public class LabelAmountPercentDto
{
    public string Label { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Percent { get; set; } = null!;
}

public class LabelValueDto
{
    public string Label { get; set; } = null!;
    public decimal Value { get; set; }
}

public class MonthlyReportTxDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public string CategoryTitle { get; set; } = null!;
    public string AccountTitle { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
