using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.Dashboard;

public record GetDashboardQuery() : IRequest<Result<DashboardDto>>;
