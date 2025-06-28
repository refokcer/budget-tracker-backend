using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.BudgetPlanPage;

public record GetBudgetPlanPageQuery(int PlanId) : IRequest<Result<BudgetPlanPageDto>>;
