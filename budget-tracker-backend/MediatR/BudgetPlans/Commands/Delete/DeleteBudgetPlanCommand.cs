using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Delete;

public record DeleteBudgetPlanCommand(int Id) : IRequest<Result<bool>>;