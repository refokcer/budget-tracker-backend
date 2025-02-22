using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetAll;

public record GetAllBudgetPlanItemsQuery : IRequest<Result<IEnumerable<BudgetPlanItemDto>>>;