using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.MonthlyReport;

public record GetMonthlyReportQuery(int Month, int? Year = null) : IRequest<Result<MonthlyReportDto>>;
