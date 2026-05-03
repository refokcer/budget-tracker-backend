using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.CreateAutoMonthly;

public record CreateAutoMonthlyBudgetPlanCommand(AutoBudgetPlanRequestDto Request)
    : IRequest<Result<AutoBudgetPlanResultDto>>;
