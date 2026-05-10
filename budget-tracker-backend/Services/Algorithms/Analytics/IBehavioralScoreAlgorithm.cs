namespace budget_tracker_backend.Services.Algorithms.Analytics;

using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Algorithms;

public interface IBehavioralScoreAlgorithm : IFinancialAlgorithm
{
    Task<BehavioralScoreDto> CalculateAsync(
        IEnumerable<Account> accounts,
        DateTime currentMonthStart,
        CancellationToken cancellationToken);
}
