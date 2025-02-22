using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetById;

public record GetBudgetPlanItemByIdQuery(int Id) : IRequest<Result<BudgetPlanItemDto>>;