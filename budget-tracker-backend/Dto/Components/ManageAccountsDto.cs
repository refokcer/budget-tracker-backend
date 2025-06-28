namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Accounts;

/// <summary>
/// Data for manage accounts component.
/// </summary>
public class ManageAccountsDto
{
    public IEnumerable<AccountDto> Accounts { get; set; } = new List<AccountDto>();
}

