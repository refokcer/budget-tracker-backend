using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetForecast;

public class GetFinancialGoalForecastHandler : IRequestHandler<GetFinancialGoalForecastQuery, Result<FinancialGoalForecastDto>>
{
    private readonly IFinancialGoalManager _manager;

    public GetFinancialGoalForecastHandler(IFinancialGoalManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<FinancialGoalForecastDto>> Handle(GetFinancialGoalForecastQuery request, CancellationToken cancellationToken)
    {
        var forecast = await _manager.GetForecastAsync(request.GoalId, cancellationToken);
        return Result.Ok(forecast);
    }
}
