using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Services.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetById;

public class GetBudgetPlanItemByIdHandler : IRequestHandler<GetBudgetPlanItemByIdQuery, Result<BudgetPlanItemDto>>
{
    private readonly IBudgetPlanItemManager _manager;
    private readonly IMapper _mapper;

    public GetBudgetPlanItemByIdHandler(IBudgetPlanItemManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(GetBudgetPlanItemByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result.Fail($"BudgetPlanItem with Id={request.Id} not found");

        var dto = _mapper.Map<BudgetPlanItemDto>(entity);
        return Result.Ok(dto);
    }
}