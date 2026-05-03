using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.Delete;

public record DeleteFinancialGoalCommand(int Id) : IRequest<Result<bool>>;
