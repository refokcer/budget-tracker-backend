namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Dto.BudgetPlans;

/// <summary>
/// Данi для модального вiкна додавання витрати.
/// </summary>
public class ExpenseModalDto
{
    public IEnumerable<CurrencyDto> Currencies { get; set; } = new List<CurrencyDto>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IEnumerable<AccountDto> Accounts { get; set; } = new List<AccountDto>();
    public IEnumerable<BudgetPlanDto> Plans { get; set; } = new List<BudgetPlanDto>();
}
