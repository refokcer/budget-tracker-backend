using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetForecast;

public record GetFinancialGoalForecastQuery(int GoalId) : IRequest<Result<FinancialGoalForecastDto>>;
