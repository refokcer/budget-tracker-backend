namespace budget_tracker_backend.Services.Algorithms.Analytics;

using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Algorithms;

public interface IFinancialStabilityAlgorithm : IFinancialAlgorithm
{
    Task<FinancialStabilityDto> CalculateAsync(
        IEnumerable<Account> accounts,
        decimal totalBalance,
        DateTime currentMonthStart,
        CancellationToken cancellationToken);
}
