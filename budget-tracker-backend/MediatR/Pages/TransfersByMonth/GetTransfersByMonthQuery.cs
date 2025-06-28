using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Pages;

namespace budget_tracker_backend.MediatR.Pages.TransfersByMonth;

/// <param name="Month">1–12</param>
/// <param name="Year">Опціонально. Якщо null — береться поточний рік</param>
public record GetTransfersByMonthQuery(int Month, int? Year = null) : IRequest<Result<TransfersByMonthDto>>;
