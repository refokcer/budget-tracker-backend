using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Delete;

public record DeleteBudgetPlanItemCommand(int Id) : IRequest<Result<bool>>;