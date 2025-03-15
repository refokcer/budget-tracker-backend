using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetByPlanId;

public record GetAllBudgetPlanItemsByPlanIdQuery(int PlanId) : IRequest<Result<IEnumerable<BudgetPlanItemDto>>>;