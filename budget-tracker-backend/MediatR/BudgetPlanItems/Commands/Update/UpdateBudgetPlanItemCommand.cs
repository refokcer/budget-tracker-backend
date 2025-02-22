using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Update;

public record UpdateBudgetPlanItemCommand(BudgetPlanItemDto ItemDto) : IRequest<Result<BudgetPlanItemDto>>;