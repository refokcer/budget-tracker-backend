using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetMonths;

public record GetAllMonthlyPlansQuery() : IRequest<Result<IEnumerable<BudgetPlanDto>>>;
