namespace budget_tracker_backend.Dto.Pages;

/// <summary>
/// Модель сторінки «Перекази за місяць». —Для віддачі у фронт.
/// </summary>
public class TransfersByMonthDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<TransferTxDto> Transactions { get; set; } = new();
}

/// <summary>
/// Коротка проекція транзакцій-переказів.
/// </summary>
public class TransferTxDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencySymbol { get; set; } = null!;
    public string CategoryTitle { get; set; } = null!;
    public string AccountFromTitle { get; set; } = null!;
    public string AccountToTitle { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
