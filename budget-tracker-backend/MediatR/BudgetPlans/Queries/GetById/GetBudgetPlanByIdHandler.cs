using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetById;

public class GetBudgetPlanByIdHandler
    : IRequestHandler<GetBudgetPlanByIdQuery, Result<BudgetPlanDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetBudgetPlanByIdHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(GetBudgetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _context.BudgetPlans.FindAsync(new object[] { request.Id }, cancellationToken);
        if (plan == null)
        {
            return Result.Fail($"BudgetPlan with Id={request.Id} not found");
        }

        var dto = _mapper.Map<BudgetPlanDto>(plan);
        return Result.Ok(dto);
    }
}