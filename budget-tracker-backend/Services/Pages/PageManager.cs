namespace budget_tracker_backend.Services.Pages;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

public class PageManager : IPageManager
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly IAccountManager _accountManager;

    public PageManager(IApplicationDbContext ctx, IMapper mapper, IAccountManager accountManager)
    {
        _ctx = ctx;
        _mapper = mapper;
        _accountManager = accountManager;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var accounts = await _accountManager.GetAllAsync(ct);
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

        return new DashboardDto
        {
            Accounts = accountDtos,
            TotalBalance = totalBalance,
            TopExpenses = expDtos,
            TopIncomes = incDtos,
            BiggestTransaction = bigDto
        };
    }

    public async Task<BudgetPlanPageDto> GetBudgetPlanPageAsync(int planId, CancellationToken ct)
    {
        var plan = await _ctx.BudgetPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan == null)
            throw new Exception($"Budget plan {planId} not found");

        var items = await _ctx.BudgetPlanItems
            .Include(i => i.Category)
            .Include(i => i.Currency)
            .Where(i => i.BudgetPlanId == planId)
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

        return dto;
    }

    public async Task<IncomesByMonthDto> GetIncomesByMonthAsync(int month, int? year, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new Exception("Month must be 1-12");

        var yr = year ?? DateTime.Today.Year;
        var start = new DateTime(yr, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var tx = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.ToAccount)
            .Where(t => t.Type == TransactionCategoryType.Income && t.Date >= start && t.Date < end)
            .AsNoTracking()
            .ToListAsync(ct);

        return new IncomesByMonthDto
        {
            Start = start,
            End = end,
            Transactions = _mapper.Map<List<IncomeTxDto>>(tx)
        };
    }

    public async Task<ExpensesByMonthDto> GetExpensesByMonthAsync(int month, int? year, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new Exception("Month must be 1-12");

        var yr = year ?? DateTime.Today.Year;
        var start = new DateTime(yr, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var tx = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.FromAccount)
            .Where(t => t.Type == TransactionCategoryType.Expense && t.Date >= start && t.Date < end)
            .AsNoTracking()
            .ToListAsync(ct);

        return new ExpensesByMonthDto
        {
            Start = start,
            End = end,
            Transactions = _mapper.Map<List<ExpenseTxDto>>(tx)
        };
    }

    public async Task<TransfersByMonthDto> GetTransfersByMonthAsync(int month, int? year, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new Exception("Month must be 1-12");

        var yr = year ?? DateTime.Today.Year;
        var start = new DateTime(yr, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var tx = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.Type == TransactionCategoryType.Transaction && t.Date >= start && t.Date < end)
            .AsNoTracking()
            .ToListAsync(ct);

        return new TransfersByMonthDto
        {
            Start = start,
            End = end,
            Transactions = _mapper.Map<List<TransferTxDto>>(tx)
        };
    }

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(int month, int? year, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new Exception("Month must be 1-12");

        var yr = year ?? DateTime.Today.Year;
        var start = new DateTime(yr, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var currency = await _ctx.Currencies.FirstOrDefaultAsync(c => c.IsBase, ct);
        var defaultCurrency = currency?.Code ?? string.Empty;

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

        return new MonthlyReportDto
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
    }
}
