using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetAll;

public class GetAllBudgetPlansHandler
    : IRequestHandler<GetAllBudgetPlansQuery, Result<IEnumerable<BudgetPlanDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllBudgetPlansHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanDto>>> Handle(GetAllBudgetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _context.BudgetPlans
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<BudgetPlanDto>>(plans);
        return Result.Ok(dtos);
    }
}