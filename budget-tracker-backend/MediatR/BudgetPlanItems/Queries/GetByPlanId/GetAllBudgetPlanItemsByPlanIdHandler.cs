using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetByPlanId;

public class GetAllBudgetPlanItemsByPlanIdHandler : IRequestHandler<GetAllBudgetPlanItemsByPlanIdQuery, Result<IEnumerable<BudgetPlanItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllBudgetPlanItemsByPlanIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<BudgetPlanItemDto>>> Handle(
        GetAllBudgetPlanItemsByPlanIdQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.BudgetPlanItems
            .Where(i => i.BudgetPlanId == request.PlanId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Mapim in DTO
        var dtos = _mapper.Map<IEnumerable<BudgetPlanItemDto>>(items);
        return Result.Ok(dtos);
    }
}