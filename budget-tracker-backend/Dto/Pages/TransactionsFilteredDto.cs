namespace budget_tracker_backend.Dto.Pages;

public class TransactionsFilteredDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<FilteredTxDto> Transactions { get; set; } = new();
}

public class FilteredTxDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public string CategoryTitle { get; set; } = null!;
    public string AccountFromTitle { get; set; } = null!;
    public string AccountToTitle { get; set; } = null!;
    public string BudetPlanTitle { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
