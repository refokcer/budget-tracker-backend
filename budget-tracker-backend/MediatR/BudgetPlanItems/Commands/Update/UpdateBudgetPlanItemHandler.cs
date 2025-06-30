using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Services.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Update;

public class UpdateBudgetPlanItemHandler : IRequestHandler<UpdateBudgetPlanItemCommand, Result<BudgetPlanItemDto>>
{
    private readonly IBudgetPlanItemManager _manager;
    private readonly IMapper _mapper;

    public UpdateBudgetPlanItemHandler(IBudgetPlanItemManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(UpdateBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ItemDto;

        var entity = await _manager.UpdateAsync(dto, cancellationToken);
        var updatedDto = _mapper.Map<BudgetPlanItemDto>(entity);
        return Result.Ok(updatedDto);
    }
}