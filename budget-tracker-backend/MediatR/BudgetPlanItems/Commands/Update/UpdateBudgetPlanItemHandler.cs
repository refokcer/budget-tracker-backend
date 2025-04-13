using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Update;

public class UpdateBudgetPlanItemHandler : IRequestHandler<UpdateBudgetPlanItemCommand, Result<BudgetPlanItemDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateBudgetPlanItemHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(UpdateBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ItemDto;

        // Looking for an existing record
        var existing = await _context.BudgetPlanItems.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            return Result.Fail($"BudgetPlanItem with Id={dto.Id} not found");
        }

        // Map fields from dto -> existing 
        _mapper.Map(dto, existing);

        _context.BudgetPlanItems.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update BudgetPlanItem");
        }

        var updatedDto = _mapper.Map<BudgetPlanItemDto>(existing);
        return Result.Ok(updatedDto);
    }
}