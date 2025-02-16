using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Update;

public record UpdateBudgetPlanCommand(BudgetPlanDto PlanToUpdate) : IRequest<Result<BudgetPlanDto>>;