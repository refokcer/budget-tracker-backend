using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.IncomesByMonth;

/// <param name="Month">1–12</param>
/// <param name="Year">Опціонально. Якщо null — береться поточний рік</param>
public record GetIncomesByMonthQuery(int Month, int? Year = null) : IRequest<Result<IncomesByMonthDto>>;
