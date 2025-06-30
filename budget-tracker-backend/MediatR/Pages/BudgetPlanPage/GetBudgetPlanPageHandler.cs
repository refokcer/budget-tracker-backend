using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Pages.BudgetPlanPage;

public class GetBudgetPlanPageHandler : IRequestHandler<GetBudgetPlanPageQuery, Result<BudgetPlanPageDto>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetBudgetPlanPageHandler(IApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx; _mapper = mapper;
    }

    public async Task<Result<BudgetPlanPageDto>> Handle(GetBudgetPlanPageQuery rq, CancellationToken ct)
    {
        var plan = await _ctx.BudgetPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == rq.PlanId, ct);
        if (plan == null)
            return Result.Fail($"Budget plan {rq.PlanId} not found");

        var items = await _ctx.BudgetPlanItems
            .Include(i => i.Category)
            .Include(i => i.Currency)
            .Where(i => i.BudgetPlanId == rq.PlanId)
            .AsNoTracking()
            .ToListAsync(ct);

        var categoryIds = items.Select(i => i.CategoryId).ToList();
        var txSums = await _ctx.Transactions
            .Where(t => t.CategoryId != null && categoryIds.Contains(t.CategoryId.Value) &&
                        t.Date >= plan.StartDate && t.Date <= plan.EndDate &&
                        t.Type == TransactionCategoryType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CatId = g.Key!.Value, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var spentByCat = txSums.ToDictionary(x => x.CatId, x => x.Sum);

        var dto = new BudgetPlanPageDto
        {
            Plan = _mapper.Map<budget_tracker_backend.Dto.BudgetPlans.BudgetPlanDto>(plan),
            Items = new()
        };

        foreach (var item in items)
        {
            var itemDto = _mapper.Map<BudgetPlanPageItemDto>(item);
            var spent = spentByCat.TryGetValue(item.CategoryId, out var s) ? s : 0m;
            itemDto.Spent = spent;
            itemDto.Remaining = item.Amount - spent;
            dto.Items.Add(itemDto);
        }

        return Result.Ok(dto);
    }
}
