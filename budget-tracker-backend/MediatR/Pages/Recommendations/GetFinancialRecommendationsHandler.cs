using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Pages;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Pages.Recommendations;

public class GetFinancialRecommendationsHandler
    : IRequestHandler<GetFinancialRecommendationsQuery, Result<FinancialRecommendationsDto>>
{
    private readonly IPageManager _manager;

    public GetFinancialRecommendationsHandler(IPageManager manager)
    {
        _manager = manager;
    }

    public async Task<Result<FinancialRecommendationsDto>> Handle(
        GetFinancialRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        return Result.Ok(await _manager.GetFinancialRecommendationsAsync(cancellationToken));
    }
}
