namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;

/// <summary>
/// Data for edit budget plan modal.
/// </summary>
public class EditPlanModalDto
{
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IEnumerable<CurrencyDto> Currencies { get; set; } = new List<CurrencyDto>();
}

