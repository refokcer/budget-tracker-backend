namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;

/// <summary>
/// Данi для модального вiкна переказу коштiв.
/// </summary>
public class TransferModalDto
{
    public IEnumerable<CurrencyDto> Currencies { get; set; } = new List<CurrencyDto>();
    public IEnumerable<AccountDto> Accounts { get; set; } = new List<AccountDto>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
}
