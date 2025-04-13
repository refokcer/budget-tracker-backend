using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Create;

public class CreateBudgetPlanHandler : IRequestHandler<CreateBudgetPlanCommand, Result<BudgetPlanDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateBudgetPlanHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(CreateBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        // Convert DTO → entity
        var entity = _mapper.Map<BudgetPlan>(request.NewPlan);
        if (entity == null)
        {
            return Result.Fail("Failed to map CreateBudgetPlanDto to BudgetPlan");
        }

        _context.BudgetPlans.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to create BudgetPlan in database");
        }

        // Map back to DTO for a response
        var resultDto = _mapper.Map<BudgetPlanDto>(entity);
        return Result.Ok(resultDto);
    }
}
