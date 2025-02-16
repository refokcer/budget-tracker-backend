using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetById;

public record GetBudgetPlanByIdQuery(int Id) : IRequest<Result<BudgetPlanDto>>;