using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Pages.Dashboard;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetDashboardHandler(IApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx; _mapper = mapper;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery rq, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var accounts = await _ctx.Accounts
            .Include(a => a.Currency)
            .AsNoTracking()
            .ToListAsync(ct);

        var accountDtos = _mapper.Map<List<DashboardAccountDto>>(accounts);
        var totalBalance = accounts.Sum(a => a.Amount);

        var txQuery = _ctx.Transactions
            .Include(t => t.Category)
            .Include(t => t.Currency)
            .Where(t => t.Date >= start && t.Date < end)
            .AsNoTracking();

        var expGroups = await txQuery
            .Where(t => t.Type == TransactionCategoryType.Expense && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Title })
            .Select(g => new { g.Key.Title, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Sum)
            .Take(10)
            .ToListAsync(ct);
        var totalExp = expGroups.Sum(g => g.Sum);
        var expDtos = expGroups.Select(g => new DashboardCategoryDto
        {
            CategoryTitle = g.Title,
            Amount = g.Sum,
            Percent = totalExp == 0 ? "0%" : $"{Math.Round(g.Sum / totalExp * 100, 2)}%"
        }).ToList();

        var incGroups = await txQuery
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Title })
            .Select(g => new { g.Key.Title, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Sum)
            .Take(10)
            .ToListAsync(ct);
        var totalInc = incGroups.Sum(g => g.Sum);
        var incDtos = incGroups.Select(g => new DashboardCategoryDto
        {
            CategoryTitle = g.Title,
            Amount = g.Sum,
            Percent = totalInc == 0 ? "0%" : $"{Math.Round(g.Sum / totalInc * 100, 2)}%"
        }).ToList();

        var biggestTx = await txQuery
            .OrderByDescending(t => t.Amount)
            .FirstOrDefaultAsync(ct);
        DashboardTransactionDto? bigDto = null;
        if (biggestTx != null)
        {
            bigDto = new DashboardTransactionDto
            {
                Title = biggestTx.Title,
                Amount = biggestTx.Amount,
                CurrencySymbol = biggestTx.Currency!.Symbol.ToString(),
                Date = biggestTx.Date
            };
        }

        var dto = new DashboardDto
        {
            Accounts = accountDtos,
            TotalBalance = totalBalance,
            TopExpenses = expDtos,
            TopIncomes = incDtos,
            BiggestTransaction = bigDto
        };

        return Result.Ok(dto);
    }
}
