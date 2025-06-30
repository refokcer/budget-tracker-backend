using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetAll;

public class GetAllBudgetPlanItemsHandler : IRequestHandler<GetAllBudgetPlanItemsQuery, Result<IEnumerable<BudgetPlanItemDto>>>
{
    private readonly IBudgetPlanItemManager _manager;
    private readonly IMapper _mapper;

    public GetAllBudgetPlanItemsHandler(IBudgetPlanItemManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanItemDto>>> Handle(GetAllBudgetPlanItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<BudgetPlanItemDto>>(items);
        return Result.Ok(dtos);
    }
}