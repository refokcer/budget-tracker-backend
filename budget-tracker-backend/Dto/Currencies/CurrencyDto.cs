namespace budget_tracker_backend.Dto.Currencies;

public class CurrencyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public bool IsBase { get; set; }
}