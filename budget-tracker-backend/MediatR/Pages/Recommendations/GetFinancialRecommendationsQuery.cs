using budget_tracker_backend.Dto.Pages;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Pages.Recommendations;

public record GetFinancialRecommendationsQuery : IRequest<Result<FinancialRecommendationsDto>>;
