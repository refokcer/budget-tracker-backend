using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetAll;

public record GetAllBudgetPlansQuery : IRequest<Result<IEnumerable<BudgetPlanDto>>>;
