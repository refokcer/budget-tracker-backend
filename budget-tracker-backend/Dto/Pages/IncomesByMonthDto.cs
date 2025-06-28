namespace budget_tracker_backend.Dto.Pages;

/// <summary>
/// Модель сторінки «Надходження за місяць». —Для віддачі у фронт.
/// </summary>
public class IncomesByMonthDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<IncomeTxDto> Transactions { get; set; } = new();
}

/// <summary>
/// Коротка проекція транзакцій-надходжень.
/// </summary>
public class IncomeTxDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public string CategoryTitle { get; set; } = null!;
    public string AccountTitle { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
