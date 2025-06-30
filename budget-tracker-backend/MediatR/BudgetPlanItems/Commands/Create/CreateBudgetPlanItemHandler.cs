using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Create;

public class CreateBudgetPlanItemHandler : IRequestHandler<CreateBudgetPlanItemCommand, Result<BudgetPlanItemDto>>
{
    private readonly IBudgetPlanItemManager _manager;
    private readonly IMapper _mapper;

    public CreateBudgetPlanItemHandler(IBudgetPlanItemManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(CreateBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.CreateAsync(request.ItemDto, cancellationToken);
        var itemDto = _mapper.Map<BudgetPlanItemDto>(entity);
        return Result.Ok(itemDto);
    }
}
