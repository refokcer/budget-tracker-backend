using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.Create;

public record CreateFinancialGoalCommand(CreateFinancialGoalDto Goal) : IRequest<Result<FinancialGoalDto>>;
