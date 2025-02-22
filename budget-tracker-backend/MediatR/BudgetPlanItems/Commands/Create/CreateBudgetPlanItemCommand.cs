using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Create;

public record CreateBudgetPlanItemCommand(CreateBudgetPlanItemDto ItemDto) : IRequest<Result<BudgetPlanItemDto>>;
