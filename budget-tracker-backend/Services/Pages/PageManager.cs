namespace budget_tracker_backend.Services.Pages;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Services.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Services.FinancialGoals;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

public class PageManager : IPageManager
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly IAccountManager _accountManager;
    private readonly IBudgetPlanManager _budgetPlanManager;
    private readonly IBudgetPlanItemManager _budgetPlanItemManager;
    private readonly ITransactionManager _transactionManager;
    private readonly IFinancialGoalManager _financialGoalManager;

    public PageManager(
        IApplicationDbContext ctx,
        IMapper mapper,
        IAccountManager accountManager,
        IBudgetPlanManager budgetPlanManager,
        IBudgetPlanItemManager budgetPlanItemManager,
        ITransactionManager transactionManager,
        IFinancialGoalManager financialGoalManager)
    {
        _ctx = ctx;
        _mapper = mapper;
        _accountManager = accountManager;
        _budgetPlanManager = budgetPlanManager;
        _budgetPlanItemManager = budgetPlanItemManager;
        _transactionManager = transactionManager;
        _financialGoalManager = financialGoalManager;
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

        var financialStability = await CalculateFinancialStabilityAsync(accounts, totalBalance, start, ct);
        var financialGoals = await GetDashboardFinancialGoalsAsync(ct);

        return new DashboardDto
        {
            Accounts = accountDtos,
            TotalBalance = totalBalance,
            TopExpenses = expDtos,
            TopIncomes = incDtos,
            BiggestTransaction = bigDto,
            FinancialStability = financialStability,
            FinancialGoals = financialGoals
        };
    }

    private async Task<List<DashboardFinancialGoalDto>> GetDashboardFinancialGoalsAsync(CancellationToken ct)
    {
        var goals = await _ctx.FinancialGoals
            .AsNoTracking()
            .OrderBy(g => g.TargetDate)
            .Take(5)
            .ToListAsync(ct);

        var dashboardGoals = new List<DashboardFinancialGoalDto>(goals.Count);
        foreach (var goal in goals)
        {
            var forecast = await _financialGoalManager.GetForecastAsync(goal.Id, ct);
            dashboardGoals.Add(new DashboardFinancialGoalDto
            {
                Id = goal.Id,
                Title = goal.Title,
                TargetAmount = forecast.TargetAmount,
                CurrentSavedAmount = forecast.CurrentSavedAmount,
                RemainingAmount = forecast.RemainingAmount,
                ProgressRatio = forecast.ProgressRatio,
                RequiredMonthlyContribution = forecast.RequiredMonthlyContribution,
                IsAchievable = forecast.IsAchievable,
                IsOffTrack = forecast.IsOffTrack,
                RiskLevel = forecast.RiskLevel,
                TargetDate = goal.TargetDate
            });
        }

        return dashboardGoals;
    }

    private async Task<FinancialStabilityDto> CalculateFinancialStabilityAsync(
        IEnumerable<Account> accounts,
        decimal totalBalance,
        DateTime currentMonthStart,
        CancellationToken ct)
    {
        var currentMonthEnd = currentMonthStart.AddMonths(1);
        var analysisStart = currentMonthStart.AddMonths(-5);
        var analysisEnd = currentMonthEnd;

        var transactions = await _ctx.Transactions
            .Where(t => t.Date >= analysisStart && t.Date < analysisEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        var monthStarts = Enumerable.Range(0, 6)
            .Select(analysisStart.AddMonths)
            .ToList();

        var monthlyCashFlow = monthStarts
            .Select(month =>
            {
                var nextMonth = month.AddMonths(1);
                var monthTransactions = transactions
                    .Where(t => t.Date >= month && t.Date < nextMonth)
                    .ToList();

                return new
                {
                    Income = monthTransactions
                        .Where(t => t.Type == TransactionCategoryType.Income)
                        .Sum(t => t.Amount),
                    Expenses = monthTransactions
                        .Where(t => t.Type == TransactionCategoryType.Expense)
                        .Sum(t => t.Amount)
                };
            })
            .ToList();

        var currentMonthIncome = monthlyCashFlow[^1].Income;
        var currentMonthExpenses = monthlyCashFlow[^1].Expenses;
        var averageMonthlyIncome = monthlyCashFlow.Average(x => x.Income);
        var averageMonthlyExpenses = monthlyCashFlow.Average(x => x.Expenses);

        var activeMonthlyPlans = await _ctx.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate < currentMonthEnd
                && p.EndDate >= currentMonthStart)
            .AsNoTracking()
            .ToListAsync(ct);

        var mandatoryCategoryIds = activeMonthlyPlans
            .SelectMany(p => p.Items ?? [])
            .Select(i => i.CategoryId)
            .Distinct()
            .ToHashSet();

        var mandatoryExpenses = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date >= currentMonthStart
                && t.Date < currentMonthEnd
                && t.CategoryId != null
                && mandatoryCategoryIds.Contains(t.CategoryId.Value))
            .Sum(t => t.Amount);

        var savingsAccountIds = accounts
            .Where(IsSavingsAccount)
            .Select(a => a.Id)
            .ToHashSet();

        var transfersToSavings = transactions
            .Where(t => t.Type == TransactionCategoryType.Transaction
                && t.Date >= currentMonthStart
                && t.Date < currentMonthEnd
                && t.AccountTo != null
                && savingsAccountIds.Contains(t.AccountTo.Value))
            .Sum(t => t.Amount);

        var netSavings = Math.Max(0m, currentMonthIncome - currentMonthExpenses);
        var mandatoryExpensesShare = currentMonthIncome > 0m
            ? Clamp(mandatoryExpenses / currentMonthIncome)
            : 0m;
        var savingsShare = currentMonthIncome > 0m
            ? Clamp(Math.Max(netSavings, transfersToSavings) / currentMonthIncome)
            : 0m;
        var emergencyFundMonths = averageMonthlyExpenses > 0m
            ? Math.Round(totalBalance / averageMonthlyExpenses, 2)
            : totalBalance > 0m ? 6m : 0m;

        var overspendingFrequency = await CalculateOverspendingFrequencyAsync(
            analysisStart,
            analysisEnd,
            ct);
        var incomeStability = CalculateIncomeStability(monthlyCashFlow.Select(x => x.Income));
        var goalAchievementIndex = await CalculateBudgetGoalAchievementIndexAsync(
            analysisStart,
            analysisEnd,
            ct);

        var mandatoryExpensesScore = 1m - mandatoryExpensesShare;
        var savingsScore = Clamp(savingsShare / 0.2m);
        var emergencyFundScore = Clamp(emergencyFundMonths / 6m);
        var overspendingScore = 1m - overspendingFrequency;

        var index = (int)Math.Round(100m * (
            mandatoryExpensesScore * 0.20m
            + savingsScore * 0.20m
            + emergencyFundScore * 0.20m
            + overspendingScore * 0.15m
            + incomeStability * 0.10m
            + goalAchievementIndex * 0.15m));

        index = (int)Math.Clamp(index, 0, 100);

        var metrics = new FinancialStabilityMetricsDto
        {
            MandatoryExpensesShare = Math.Round(mandatoryExpensesShare, 4),
            SavingsShare = Math.Round(savingsShare, 4),
            EmergencyFundMonths = emergencyFundMonths,
            OverspendingFrequency = Math.Round(overspendingFrequency, 4),
            IncomeStability = Math.Round(incomeStability, 4),
            GoalAchievementIndex = Math.Round(goalAchievementIndex, 4),
            AverageMonthlyIncome = Math.Round(averageMonthlyIncome, 2),
            AverageMonthlyExpenses = Math.Round(averageMonthlyExpenses, 2)
        };

        return new FinancialStabilityDto
        {
            Index = index,
            Level = GetFinancialStabilityLevel(index),
            Metrics = metrics,
            Recommendations = BuildFinancialStabilityRecommendations(metrics)
        };
    }

    private async Task<decimal> CalculateOverspendingFrequencyAsync(
        DateTime analysisStart,
        DateTime analysisEnd,
        CancellationToken ct)
    {
        var plans = await _ctx.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.StartDate < analysisEnd && p.EndDate >= analysisStart)
            .AsNoTracking()
            .ToListAsync(ct);

        if (plans.Count == 0)
            return 0m;

        var planIds = plans.Select(p => p.Id).ToList();
        var spentByPlan = await _ctx.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.BudgetPlanId != null
                && planIds.Contains(t.BudgetPlanId.Value)
                && t.Date >= analysisStart
                && t.Date < analysisEnd)
            .GroupBy(t => t.BudgetPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Amount = g.Sum(t => t.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.PlanId, x => x.Amount, ct);

        var overspentPlans = plans.Count(plan =>
        {
            var plannedAmount = (plan.Items ?? []).Sum(i => i.Amount);
            if (plannedAmount <= 0m)
                return false;

            return spentByPlan.TryGetValue(plan.Id, out var spent) && spent > plannedAmount;
        });

        return Clamp((decimal)overspentPlans / plans.Count);
    }

    private async Task<decimal> CalculateBudgetGoalAchievementIndexAsync(
        DateTime analysisStart,
        DateTime analysisEnd,
        CancellationToken ct)
    {
        var plans = await _ctx.BudgetPlans
            .Include(p => p.Items)
            .Where(p => p.StartDate < analysisEnd && p.EndDate >= analysisStart)
            .AsNoTracking()
            .ToListAsync(ct);

        var planItems = plans
            .SelectMany(plan => (plan.Items ?? [])
                .Select(item => new
                {
                    PlanId = plan.Id,
                    item.CategoryId,
                    item.Amount
                }))
            .Where(item => item.Amount > 0m)
            .ToList();

        if (planItems.Count == 0)
            return 1m;

        var planIds = plans.Select(p => p.Id).ToList();
        var spentByPlanCategory = await _ctx.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.BudgetPlanId != null
                && t.CategoryId != null
                && planIds.Contains(t.BudgetPlanId.Value)
                && t.Date >= analysisStart
                && t.Date < analysisEnd)
            .GroupBy(t => new { PlanId = t.BudgetPlanId!.Value, CategoryId = t.CategoryId!.Value })
            .Select(g => new
            {
                g.Key.PlanId,
                g.Key.CategoryId,
                Amount = g.Sum(t => t.Amount)
            })
            .AsNoTracking()
            .ToDictionaryAsync(x => (x.PlanId, x.CategoryId), x => x.Amount, ct);

        var totalPlannedAmount = planItems.Sum(i => i.Amount);
        var weightedScore = planItems.Sum(item =>
        {
            spentByPlanCategory.TryGetValue((item.PlanId, item.CategoryId), out var spent);

            var categoryScore = spent <= item.Amount || spent == 0m
                ? 1m
                : Clamp(item.Amount / spent);

            return categoryScore * item.Amount;
        });

        return totalPlannedAmount > 0m
            ? Clamp(weightedScore / totalPlannedAmount)
            : 1m;
    }

    private static decimal CalculateIncomeStability(IEnumerable<decimal> monthlyIncomes)
    {
        var incomes = monthlyIncomes.ToList();
        var averageIncome = incomes.Average();

        if (averageIncome <= 0m)
            return 0m;

        var variance = incomes
            .Select(income => Math.Pow((double)(income - averageIncome), 2))
            .Average();
        var coefficientOfVariation = (decimal)(Math.Sqrt(variance) / (double)averageIncome);

        return 1m - Clamp(coefficientOfVariation);
    }

    private static List<string> BuildFinancialStabilityRecommendations(FinancialStabilityMetricsDto metrics)
    {
        var recommendations = new List<string>();

        if (metrics.MandatoryExpensesShare > 0.5m)
            recommendations.Add("Зменшіть постійні витрати або перегляньте регулярні зобов'язання.");

        if (metrics.SavingsShare < 0.1m)
            recommendations.Add("Відкладайте щонайменше 10% місячного доходу на заощадження.");

        if (metrics.EmergencyFundMonths < 3m)
            recommendations.Add("Сформуйте резервний фонд, який покриває від 3 до 6 місяців витрат.");

        if (metrics.OverspendingFrequency > 0.25m)
            recommendations.Add("Перегляньте бюджетні ліміти та відстежуйте категорії з повторним перевитрачанням.");

        if (metrics.IncomeStability < 0.6m)
            recommendations.Add("Плануйте бюджет з обережними оцінками доходу, оскільки дохід нестабільний.");

        if (metrics.GoalAchievementIndex < 0.7m)
            recommendations.Add("Скоригуйте ліміти категорій або зменшіть витрати у категоріях з перевитрачанням.");

        if (recommendations.Count == 0)
            recommendations.Add("Фінансова стійкість висока. Продовжуйте дотримуватися поточної бюджетної дисципліни.");

        return recommendations;
    }

    private static string GetFinancialStabilityLevel(int index)
    {
        if (index < 40)
            return "Low";

        if (index < 70)
            return "Medium";

        return "High";
    }

    private static bool IsSavingsAccount(Account account)
    {
        return account.Type is AccountType.Savings
            or AccountType.Deposit
            or AccountType.Investment;
    }

    private static decimal Clamp(decimal value)
    {
        return Math.Min(1m, Math.Max(0m, value));
    }

    public async Task<BudgetPlanPageDto> GetBudgetPlanPageAsync(int planId, bool includeEvents, CancellationToken ct)
    {
        var plan = await _budgetPlanManager.GetByIdAsync(planId, ct);
        if (plan == null)
            throw new Exception($"Budget plan {planId} not found");

        var items = await _budgetPlanItemManager.GetByPlanIdAsync(planId, ct);

        var otherItems = items.Where(IsOtherBudgetItem).ToList();
        var plannedCategoryIds = items
            .Where(i => !IsOtherBudgetItem(i))
            .Select(i => i.CategoryId)
            .ToHashSet();
        var transactions = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Include(t => t.BudgetPlan)
            .Where(t => t.BudgetPlanId == planId)
            .AsNoTracking()
            .ToListAsync(ct);
        var txSums = transactions
            .Where(t => t.CategoryId != null && plannedCategoryIds.Contains(t.CategoryId.Value) &&
                        t.Type == TransactionCategoryType.Expense)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CatId = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToList();
        var spentByCat = txSums.ToDictionary(x => x.CatId, x => x.Sum);
        var otherSpent = transactions
            .Where(t => t.Type == TransactionCategoryType.Expense)
            .Where(t => t.CategoryId == null || !plannedCategoryIds.Contains(t.CategoryId.Value))
            .Sum(t => t.Amount);

        var dto = new BudgetPlanPageDto
        {
            Plan = _mapper.Map<budget_tracker_backend.Dto.BudgetPlans.BudgetPlanDto>(plan),
            Items = new(),
            Transactions = _mapper.Map<List<FilteredTxDto>>(transactions),
        };

        foreach (var item in items)
        {
            var itemDto = _mapper.Map<BudgetPlanPageItemDto>(item);
            var isOther = IsOtherBudgetItem(item);
            var spent = isOther
                ? otherSpent
                : spentByCat.TryGetValue(item.CategoryId, out var s) ? s : 0m;
            itemDto.Spent = spent;
            itemDto.Remaining = item.Amount - spent;
            itemDto.IsOther = isOther;
            dto.Items.Add(itemDto);
        }

        if (otherItems.Count == 0)
        {
            var baseCurrency = await _ctx.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.IsBase, ct);
            dto.Items.Add(new BudgetPlanPageItemDto
            {
                Id = -plan.Id,
                BudgetPlanId = plan.Id,
                CategoryId = 0,
                CategoryTitle = "Other",
                Amount = 0m,
                CurrencyId = baseCurrency?.Id ?? 0,
                CurrencySymbol = baseCurrency?.Symbol.ToString() ?? string.Empty,
                Spent = otherSpent,
                Remaining = -otherSpent,
                Description = otherSpent > 0m ? "Expenses from categories not included in this plan" : null,
                IsOther = true,
                IsVirtual = true
            });
        }

        if (includeEvents)
        {
            var baseCurrencySymbol = (await _ctx.Currencies.FirstOrDefaultAsync(c => c.IsBase, ct))?.Symbol.ToString() ?? string.Empty;

            var events = await _ctx.BudgetPlans
                .Where(p => p.ParentId == planId && p.Type == BudgetPlanType.Event)
                .ToListAsync(ct);

            foreach (var ev in events)
            {
                var eventPage = await GetBudgetPlanPageAsync(ev.Id, false, ct);

                var evAmount = eventPage.Items.Sum(i => i.Amount);
                var evSpent = eventPage.Items.Sum(i => i.Spent);

                dto.Items.Add(new BudgetPlanPageItemDto
                {
                    Id = ev.Id,
                    CategoryTitle = ev.Title,
                    Amount = evAmount,
                    CurrencySymbol = baseCurrencySymbol,
                    Spent = evSpent,
                    Remaining = evAmount - evSpent,
                    Description = ev.Description,
                    IsEventSummary = true
                });

                dto.Events.Add(new BudgetPlanEventDto
                {
                    Plan = eventPage.Plan,
                    Items = eventPage.Items,
                    Transactions = eventPage.Transactions
                });

            }
        }
        return dto;
    }

    private static bool IsOtherBudgetItem(BudgetPlanItem item)
    {
        return IsOtherBudgetCategoryTitle(item.Category?.Title);
    }

    private static bool IsOtherBudgetCategoryTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        var normalized = title.Trim().ToLowerInvariant();
        return normalized is "other" or "others" or "другие" or "другое" or "інше" or "інші";
    }

    public async Task<BudgetPlanPageDto> GetEventPageAsync(int eventId, CancellationToken ct)
    {
        var ev = await _budgetPlanManager.GetByIdAsync(eventId, ct);
        if (ev == null || ev.Type != BudgetPlanType.Event)
            throw new Exception($"Event {eventId} not found");

        return await GetBudgetPlanPageAsync(eventId, false, ct);
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
            .Include(t => t.BudgetPlan)
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
