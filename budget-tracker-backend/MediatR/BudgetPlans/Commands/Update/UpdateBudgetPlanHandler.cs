using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Update;

public class UpdateBudgetPlanHandler : IRequestHandler<UpdateBudgetPlanCommand, Result<BudgetPlanDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateBudgetPlanHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(UpdateBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        var dto = request.PlanToUpdate;
        var existing = await _context.BudgetPlans.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            return Result.Fail($"BudgetPlan with Id={dto.Id} not found");
        }

        _mapper.Map(dto, existing);

        _context.BudgetPlans.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update BudgetPlan");
        }

        var updatedDto = _mapper.Map<BudgetPlanDto>(existing);
        return Result.Ok(updatedDto);
    }
}