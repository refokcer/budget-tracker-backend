using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.Update;

public record UpdateFinancialGoalCommand(FinancialGoalDto Goal) : IRequest<Result<FinancialGoalDto>>;
