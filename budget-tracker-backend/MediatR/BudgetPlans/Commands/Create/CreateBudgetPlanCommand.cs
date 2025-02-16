using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Create;

public record CreateBudgetPlanCommand(CreateBudgetPlanDto NewPlan) : IRequest<Result<BudgetPlanDto>>;