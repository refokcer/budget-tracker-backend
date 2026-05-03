using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetById;

public record GetFinancialGoalByIdQuery(int Id) : IRequest<Result<FinancialGoalDto>>;
