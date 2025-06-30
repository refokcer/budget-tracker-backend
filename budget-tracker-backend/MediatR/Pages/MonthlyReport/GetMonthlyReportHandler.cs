using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Pages.MonthlyReport;

public class GetMonthlyReportHandler : IRequestHandler<GetMonthlyReportQuery, Result<MonthlyReportDto>>
{
    private readonly IApplicationDbContext _ctx;

    public GetMonthlyReportHandler(IApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Result<MonthlyReportDto>> Handle(GetMonthlyReportQuery rq, CancellationToken ct)
    {
        if (rq.Month is < 1 or > 12)
            return Result.Fail("Month must be 1-12");

        var year = rq.Year ?? DateTime.Today.Year;
        var start = new DateTime(year, rq.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var currency = await _ctx.Currencies.FirstOrDefaultAsync(c => c.IsBase, ct);
        var defaultCurrency = currency?.Code ?? "";

        var tx = await _ctx.Transactions
            .Include(t => t.Category)
            .Include(t => t.Currency)
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.Date >= start && t.Date < end)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalExp = tx.Where(t => t.Type == TransactionCategoryType.Expense).Sum(t => t.Amount);
        var totalInc = tx.Where(t => t.Type == TransactionCategoryType.Income).Sum(t => t.Amount);
        var balance = totalInc - totalExp;

        var topExpCats = tx
            .Where(t => t.Type == TransactionCategoryType.Expense && t.CategoryId != null)
            .GroupBy(t => t.Category!.Title)
            .Select(g => new { Label = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .ToList();
        var expCatTotal = topExpCats.Sum(g => g.Amount);
        var topExpDtos = topExpCats
            .Select(g => new LabelAmountPercentDto
            {
                Label = g.Label,
                Amount = g.Amount,
                Percent = expCatTotal == 0 ? "0%" : $"{Math.Round(g.Amount / expCatTotal * 100, 2)}%"
            }).ToList();

        var topIncCats = tx
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => t.Category!.Title)
            .Select(g => new { Label = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .ToList();
        var incCatTotal = topIncCats.Sum(g => g.Amount);
        var topIncDtos = topIncCats
            .Select(g => new LabelAmountPercentDto
            {
                Label = g.Label,
                Amount = g.Amount,
                Percent = incCatTotal == 0 ? "0%" : $"{Math.Round(g.Amount / incCatTotal * 100, 2)}%"
            }).ToList();

        var expByCat = tx
            .Where(t => t.Type == TransactionCategoryType.Expense && t.CategoryId != null)
            .GroupBy(t => t.Category!.Title)
            .Select(g => new LabelValueDto { Label = g.Key, Value = g.Sum(x => x.Amount) })
            .ToList();

        var incByCat = tx
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => t.Category!.Title)
            .Select(g => new LabelValueDto { Label = g.Key, Value = g.Sum(x => x.Amount) })
            .ToList();

        var expByAccount = tx
            .Where(t => t.Type == TransactionCategoryType.Expense && t.FromAccount != null)
            .GroupBy(t => t.FromAccount!.Title)
            .Select(g => new LabelValueDto { Label = g.Key, Value = g.Sum(x => x.Amount) })
            .ToList();

        var topExpenseTx = tx
            .Where(t => t.Type == TransactionCategoryType.Expense)
            .OrderByDescending(t => t.Amount)
            .FirstOrDefault();
        MonthlyReportTxDto? topTxDto = null;
        if (topExpenseTx != null)
        {
            topTxDto = new MonthlyReportTxDto
            {
                Title = topExpenseTx.Title,
                Amount = topExpenseTx.Amount,
                CurrencySymbol = topExpenseTx.Currency!.Symbol.ToString(),
                CategoryTitle = topExpenseTx.Category?.Title ?? string.Empty,
                AccountTitle = topExpenseTx.FromAccount?.Title ?? string.Empty,
                Date = topExpenseTx.Date,
                Description = topExpenseTx.Description
            };
        }

        var dto = new MonthlyReportDto
        {
            TotalExp = totalExp,
            TotalInc = totalInc,
            Balance = balance,
            DefaultCurrency = defaultCurrency,
            TopExpenseCategories = topExpDtos,
            TopIncomeCategories = topIncDtos,
            ExpensesByCategory = expByCat,
            IncomesByCategory = incByCat,
            ExpensesByAccount = expByAccount,
            TopExpenseTransaction = topTxDto
        };

        return Result.Ok(dto);
    }
}
