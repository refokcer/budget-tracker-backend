namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;

/// <summary>
/// Данi для модального вiкна додавання доходу.
/// </summary>
public class IncomeModalDto
{
    public IEnumerable<CurrencyDto> Currencies { get; set; } = new List<CurrencyDto>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IEnumerable<AccountDto> Accounts { get; set; } = new List<AccountDto>();
}
