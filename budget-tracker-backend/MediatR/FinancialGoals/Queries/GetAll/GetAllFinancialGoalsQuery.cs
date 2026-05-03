using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetAll;

public record GetAllFinancialGoalsQuery : IRequest<Result<IEnumerable<FinancialGoalDto>>>;
