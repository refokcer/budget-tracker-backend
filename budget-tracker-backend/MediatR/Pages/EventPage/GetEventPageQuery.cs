using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.EventPage;

public record GetEventPageQuery(int EventId) : IRequest<Result<BudgetPlanEventDto>>;
