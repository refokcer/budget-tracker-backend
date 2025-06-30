using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetAll;

public class GetAllBudgetPlanItemsHandler : IRequestHandler<GetAllBudgetPlanItemsQuery, Result<IEnumerable<BudgetPlanItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllBudgetPlanItemsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanItemDto>>> Handle(GetAllBudgetPlanItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.BudgetPlanItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<BudgetPlanItemDto>>(items);
        return Result.Ok(dtos);
    }
}