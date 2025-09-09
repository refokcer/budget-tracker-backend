using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlans;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetEvents;

public record GetAllEventsQuery() : IRequest<Result<IEnumerable<BudgetPlanDto>>>;
