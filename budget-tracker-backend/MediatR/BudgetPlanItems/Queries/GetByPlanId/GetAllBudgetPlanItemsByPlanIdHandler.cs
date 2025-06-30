using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetByPlanId;

public class GetAllBudgetPlanItemsByPlanIdHandler : IRequestHandler<GetAllBudgetPlanItemsByPlanIdQuery, Result<IEnumerable<BudgetPlanItemDto>>>
{
    private readonly IBudgetPlanItemManager _manager;
    private readonly IMapper _mapper;

    public GetAllBudgetPlanItemsByPlanIdHandler(IBudgetPlanItemManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanItemDto>>> Handle(
        GetAllBudgetPlanItemsByPlanIdQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _manager.GetByPlanIdAsync(request.PlanId, cancellationToken);

        // Mapim in DTO
        var dtos = _mapper.Map<IEnumerable<BudgetPlanItemDto>>(items);
        return Result.Ok(dtos);
    }
}