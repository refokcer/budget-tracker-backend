namespace budget_tracker_backend.Services.Pages;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Services.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Services.FinancialGoals;
using budget_tracker_backend.Services.Algorithms.Analytics;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.RecurringPayments;
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
    private readonly IFinancialStabilityAlgorithm _financialStabilityAlgorithm;
    private readonly IBehavioralScoreAlgorithm _behavioralScoreAlgorithm;

    public PageManager(
        IApplicationDbContext ctx,
        IMapper mapper,
        IAccountManager accountManager,
        IBudgetPlanManager budgetPlanManager,
        IBudgetPlanItemManager budgetPlanItemManager,
        ITransactionManager transactionManager,
        IFinancialGoalManager financialGoalManager,
        IFinancialStabilityAlgorithm financialStabilityAlgorithm,
        IBehavioralScoreAlgorithm behavioralScoreAlgorithm)
    {
        _ctx = ctx;
        _mapper = mapper;
        _accountManager = accountManager;
        _budgetPlanManager = budgetPlanManager;
        _budgetPlanItemManager = budgetPlanItemManager;
        _transactionManager = transactionManager;
        _financialGoalManager = financialGoalManager;
        _financialStabilityAlgorithm = financialStabilityAlgorithm;
        _behavioralScoreAlgorithm = behavioralScoreAlgorithm;
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
            .GroupBy(t => new { t.CategoryId, t.Category!.Title, t.Category.Color })
            .Select(g => new { g.Key.Title, g.Key.Color, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Sum)
            .Take(10)
            .ToListAsync(ct);
        var totalExp = expGroups.Sum(g => g.Sum);
        var expDtos = expGroups.Select(g => new DashboardCategoryDto
        {
            CategoryTitle = g.Title,
            Amount = g.Sum,
            Percent = totalExp == 0 ? "0%" : $"{Math.Round(g.Sum / totalExp * 100, 2)}%",
            Color = g.Color
        }).ToList();

        var incGroups = await txQuery
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Title, t.Category.Color })
            .Select(g => new { g.Key.Title, g.Key.Color, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Sum)
            .Take(10)
            .ToListAsync(ct);
        var totalInc = incGroups.Sum(g => g.Sum);
        var incDtos = incGroups.Select(g => new DashboardCategoryDto
        {
            CategoryTitle = g.Title,
            Amount = g.Sum,
            Percent = totalInc == 0 ? "0%" : $"{Math.Round(g.Sum / totalInc * 100, 2)}%",
            Color = g.Color
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

        var financialStability = await _financialStabilityAlgorithm.CalculateAsync(accounts, totalBalance, start, ct);
        var behavioralScore = await _behavioralScoreAlgorithm.CalculateAsync(accounts, start, ct);
        var financialGoals = await GetDashboardFinancialGoalsAsync(ct);

        return new DashboardDto
        {
            Accounts = accountDtos,
            TotalBalance = totalBalance,
            TopExpenses = expDtos,
            TopIncomes = incDtos,
            BiggestTransaction = bigDto,
            FinancialStability = financialStability,
            BehavioralScore = behavioralScore,
            FinancialGoals = financialGoals
        };
    }

    public async Task<FinancialRecommendationsDto> GetFinancialRecommendationsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        var accounts = (await _accountManager.GetAllAsync(ct)).ToList();
        var totalBalance = accounts.Sum(a => a.Amount);
        var stability = await _financialStabilityAlgorithm.CalculateAsync(accounts, totalBalance, currentMonthStart, ct);
        var previousStability = await _financialStabilityAlgorithm.CalculateAsync(accounts, totalBalance, previousMonthStart, ct);
        var behavior = await _behavioralScoreAlgorithm.CalculateAsync(accounts, currentMonthStart, ct);
        var previousBehavior = await _behavioralScoreAlgorithm.CalculateAsync(accounts, previousMonthStart, ct);
        var goals = await GetDashboardFinancialGoalsAsync(ct);
        var currentTopExpenses = await GetCurrentMonthTopExpensesAsync(currentMonthStart, ct);

        var stabilityDelta = stability.Index - previousStability.Index;
        var behaviorDelta = behavior.Score - previousBehavior.Score;
        var priorityActions = new List<FinancialRecommendationActionDto>();

        priorityActions.AddRange(BuildStabilityActions(stability, stabilityDelta));
        priorityActions.AddRange(BuildBehaviorActions(behavior, behaviorDelta));
        priorityActions.AddRange(BuildGoalActions(goals));
        priorityActions.AddRange(BuildExpenseActions(currentTopExpenses));

        priorityActions = priorityActions
            .OrderBy(action => PriorityRank(action.Priority))
            .ThenBy(action => action.Source)
            .Take(8)
            .ToList();

        var sections = new List<FinancialRecommendationSectionDto>
        {
            new()
            {
                Title = "Financial stability",
                Status = stability.Level,
                Summary = BuildStabilitySummary(stability, stabilityDelta),
                Actions = BuildStabilityActions(stability, stabilityDelta).Take(4).ToList()
            },
            new()
            {
                Title = "Financial behavior",
                Status = behavior.Level,
                Summary = BuildBehaviorSummary(behavior, behaviorDelta),
                Actions = BuildBehaviorActions(behavior, behaviorDelta).Take(4).ToList()
            },
            new()
            {
                Title = "Goals",
                Status = goals.Any(g => g.IsOffTrack || !g.IsAchievable) ? "Needs attention" : "On track",
                Summary = BuildGoalsSummary(goals),
                Actions = BuildGoalActions(goals).Take(4).ToList()
            },
            new()
            {
                Title = "Current spending",
                Status = currentTopExpenses.Count == 0 ? "No data" : "Tracked",
                Summary = BuildExpenseSummary(currentTopExpenses),
                Actions = BuildExpenseActions(currentTopExpenses).Take(3).ToList()
            }
        };

        return new FinancialRecommendationsDto
        {
            GeneratedAt = now,
            OverallStatus = ResolveOverallStatus(stability, stabilityDelta, behavior, behaviorDelta, goals),
            Headline = BuildRecommendationsHeadline(stability, stabilityDelta, behavior, behaviorDelta, goals),
            Explanation = BuildRecommendationsExplanation(stability, stabilityDelta, behavior, behaviorDelta, goals),
            FinancialStabilityIndex = stability.Index,
            PreviousFinancialStabilityIndex = previousStability.Index,
            FinancialStabilityDelta = stabilityDelta,
            FinancialStabilityLevel = stability.Level,
            BehavioralScore = behavior.Score,
            PreviousBehavioralScore = previousBehavior.Score,
            BehavioralScoreDelta = behaviorDelta,
            BehavioralLevel = behavior.Level,
            Signals = BuildSignals(stability, previousStability, behavior, previousBehavior, goals),
            PriorityActions = priorityActions,
            Sections = sections
        };
    }

    private async Task<List<DashboardCategoryDto>> GetCurrentMonthTopExpensesAsync(
        DateTime currentMonthStart,
        CancellationToken ct)
    {
        var currentMonthEnd = currentMonthStart.AddMonths(1);
        var groups = await _ctx.Transactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date >= currentMonthStart
                && t.Date < currentMonthEnd
                && t.CategoryId != null)
            .GroupBy(t => new { t.Category!.Title, t.Category.Color })
            .Select(g => new { g.Key.Title, g.Key.Color, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(5)
            .AsNoTracking()
            .ToListAsync(ct);

        var total = groups.Sum(g => g.Amount);
        return groups.Select(g => new DashboardCategoryDto
        {
            CategoryTitle = g.Title,
            Amount = g.Amount,
            Percent = total <= 0m ? "0%" : $"{Math.Round(g.Amount / total * 100, 2)}%",
            Color = g.Color
        }).ToList();
    }

    private static List<FinancialRecommendationSignalDto> BuildSignals(
        FinancialStabilityDto stability,
        FinancialStabilityDto previousStability,
        BehavioralScoreDto behavior,
        BehavioralScoreDto previousBehavior,
        List<DashboardFinancialGoalDto> goals)
    {
        return
        [
            new()
            {
                Title = "Financial stability",
                Value = stability.Index.ToString(),
                PreviousValue = previousStability.Index.ToString(),
                Level = stability.Level,
                Explanation = BuildStabilitySummary(stability, stability.Index - previousStability.Index)
            },
            new()
            {
                Title = "Behavioral score",
                Value = behavior.Score.ToString(),
                PreviousValue = previousBehavior.Score.ToString(),
                Level = behavior.Level,
                Explanation = BuildBehaviorSummary(behavior, behavior.Score - previousBehavior.Score)
            },
            new()
            {
                Title = "Savings share",
                Value = FormatPercent(stability.Metrics.SavingsShare),
                PreviousValue = FormatPercent(previousStability.Metrics.SavingsShare),
                Level = stability.Metrics.SavingsShare >= 0.1m ? "Healthy" : "Weak",
                Explanation = stability.Metrics.SavingsShare >= 0.1m
                    ? "Savings pace is at or above the minimum recommended level."
                    : "Savings are below the 10% monthly income baseline."
            },
            new()
            {
                Title = "Goals at risk",
                Value = goals.Count(g => g.IsOffTrack || !g.IsAchievable).ToString(),
                Level = goals.Any(g => g.IsOffTrack || !g.IsAchievable) ? "Needs action" : "Healthy",
                Explanation = BuildGoalsSummary(goals)
            }
        ];
    }

    private static List<FinancialRecommendationActionDto> BuildStabilityActions(
        FinancialStabilityDto stability,
        int delta)
    {
        var actions = stability.Recommendations
            .Select(recommendation => new FinancialRecommendationActionDto
            {
                Title = recommendation,
                Why = BuildStabilityWhy(stability, delta),
                WhatToDo = MapRecommendationToAction(recommendation),
                Impact = "Raises the financial stability index by improving cash flow, reserve coverage, or budget adherence.",
                Priority = ResolveStabilityPriority(stability, delta),
                Source = "Financial stability",
                ActionLink = "/budget-plans"
            })
            .ToList();

        if (delta <= -5)
        {
            actions.Insert(0, new FinancialRecommendationActionDto
            {
                Title = "Investigate the stability drop",
                Why = $"The index changed by {delta} points compared with the previous month.",
                WhatToDo = "Open the current budget plan and check overspent categories, savings share, and mandatory expenses.",
                Impact = "Shows which financial driver caused the decline before the next plan is generated.",
                Priority = "High",
                Source = "Financial stability",
                ActionLink = "/budget-plans"
            });
        }

        return actions;
    }

    private static List<FinancialRecommendationActionDto> BuildBehaviorActions(
        BehavioralScoreDto behavior,
        int delta)
    {
        var actions = behavior.Insights
            .Select(insight => new FinancialRecommendationActionDto
            {
                Title = insight,
                Why = BuildBehaviorWhy(behavior, delta),
                WhatToDo = MapRecommendationToAction(insight),
                Impact = "Improves the behavioral score by reducing repeated overspending and unplanned spending.",
                Priority = ResolveBehaviorPriority(behavior, delta),
                Source = "Behavioral score",
                ActionLink = "/expenses"
            })
            .ToList();

        if (behavior.Metrics.ImpulsiveTransactions > 0)
        {
            actions.Add(new FinancialRecommendationActionDto
            {
                Title = "Review impulsive transactions",
                Why = $"{behavior.Metrics.ImpulsiveTransactions} transactions look unplanned or unusually large.",
                WhatToDo = "Filter current expenses and move repeated purchases into a planned category or reduce them next month.",
                Impact = "Improves impulse control and makes the next auto plan more accurate.",
                Priority = behavior.Metrics.ImpulseControl < 0.75m ? "High" : "Medium",
                Source = "Behavioral score",
                ActionLink = "/expenses"
            });
        }

        return actions;
    }

    private static List<FinancialRecommendationActionDto> BuildGoalActions(List<DashboardFinancialGoalDto> goals)
    {
        return goals
            .Where(goal => goal.IsOffTrack || !goal.IsAchievable)
            .Select(goal => new FinancialRecommendationActionDto
            {
                Title = $"Protect goal: {goal.Title}",
                Why = goal.IsOffTrack
                    ? "Actual progress is below the expected pace for the target date."
                    : "Forecast says the current monthly contribution is not enough.",
                WhatToDo = $"Reserve about {Math.Round(goal.RequiredMonthlyContribution, 2)} per month or apply budget adjustments from the goal page.",
                Impact = "Keeps the target date realistic and turns goal pressure into concrete category limits.",
                Priority = string.Equals(goal.RiskLevel, "High", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
                Source = "Financial goals",
                ActionLink = "/goals"
            })
            .ToList();
    }

    private static List<FinancialRecommendationActionDto> BuildExpenseActions(List<DashboardCategoryDto> topExpenses)
    {
        if (topExpenses.Count == 0)
            return [];

        var actions = new List<FinancialRecommendationActionDto>();
        var largest = topExpenses[0];
        if (ParsePercent(largest.Percent) >= 35m)
        {
            actions.Add(new FinancialRecommendationActionDto
            {
                Title = $"Check concentration in {largest.CategoryTitle}",
                Why = $"{largest.CategoryTitle} is {largest.Percent} of tracked current-month expenses.",
                WhatToDo = "Split this category into mandatory and optional parts or set a stricter limit in the next plan.",
                Impact = "Prevents one large category from hiding avoidable spending.",
                Priority = "Medium",
                Source = "Current spending",
                ActionLink = "/expenses"
            });
        }

        return actions;
    }

    private static string BuildRecommendationsHeadline(
        FinancialStabilityDto stability,
        int stabilityDelta,
        BehavioralScoreDto behavior,
        int behaviorDelta,
        List<DashboardFinancialGoalDto> goals)
    {
        var riskyGoals = goals.Count(g => g.IsOffTrack || !g.IsAchievable);
        if (stabilityDelta <= -5)
            return $"Financial stability fell by {Math.Abs(stabilityDelta)} points.";
        if (behaviorDelta <= -5)
            return $"Behavioral score fell by {Math.Abs(behaviorDelta)} points.";
        if (riskyGoals > 0)
            return $"{riskyGoals} financial goal{(riskyGoals == 1 ? " needs" : "s need")} attention.";
        if (stability.Index >= 70 && behavior.Score >= 85)
            return "Financial discipline is healthy.";
        return "Financial state is stable, but there are optimizations to apply.";
    }

    private static string BuildRecommendationsExplanation(
        FinancialStabilityDto stability,
        int stabilityDelta,
        BehavioralScoreDto behavior,
        int behaviorDelta,
        List<DashboardFinancialGoalDto> goals)
    {
        var reasons = new List<string>();

        if (stabilityDelta <= -5)
            reasons.Add(BuildStabilityWhy(stability, stabilityDelta));
        if (behaviorDelta <= -5)
            reasons.Add(BuildBehaviorWhy(behavior, behaviorDelta));
        if (goals.Any(g => g.IsOffTrack || !g.IsAchievable))
            reasons.Add(BuildGoalsSummary(goals));
        if (reasons.Count == 0)
            reasons.Add("No critical drop was detected. The recommendations focus on preserving budget discipline and improving reserve quality.");

        return string.Join(" ", reasons);
    }

    private static string BuildStabilitySummary(FinancialStabilityDto stability, int delta)
    {
        var trend = delta == 0 ? "unchanged" : delta > 0 ? $"up {delta}" : $"down {Math.Abs(delta)}";
        return $"Index is {stability.Index} ({stability.Level}), {trend} from the previous month. Savings share is {FormatPercent(stability.Metrics.SavingsShare)}, emergency fund covers {stability.Metrics.EmergencyFundMonths} months.";
    }

    private static string BuildBehaviorSummary(BehavioralScoreDto behavior, int delta)
    {
        var trend = delta == 0 ? "unchanged" : delta > 0 ? $"up {delta}" : $"down {Math.Abs(delta)}";
        return $"Score is {behavior.Score} ({behavior.Level}), {trend} from the previous month. Limit adherence is {FormatPercent(behavior.Metrics.LimitAdherence)}, impulse control is {FormatPercent(behavior.Metrics.ImpulseControl)}.";
    }

    private static string BuildGoalsSummary(List<DashboardFinancialGoalDto> goals)
    {
        if (goals.Count == 0)
            return "No active goals are tracked yet.";

        var risky = goals.Count(g => g.IsOffTrack || !g.IsAchievable);
        return risky == 0
            ? "Tracked goals are currently on pace."
            : $"{risky} of {goals.Count} tracked goals are off track or forecasted as risky.";
    }

    private static string BuildExpenseSummary(List<DashboardCategoryDto> topExpenses)
    {
        if (topExpenses.Count == 0)
            return "No current-month expense categories were found yet.";

        var largest = topExpenses[0];
        return $"Largest current-month expense category is {largest.CategoryTitle}: {largest.Percent} of tracked category spending.";
    }

    private static string BuildStabilityWhy(FinancialStabilityDto stability, int delta)
    {
        var weakMetrics = new List<string>();
        if (stability.Metrics.SavingsShare < 0.1m)
            weakMetrics.Add("low savings share");
        if (stability.Metrics.EmergencyFundMonths < 3m)
            weakMetrics.Add("weak emergency fund");
        if (stability.Metrics.OverspendingFrequency > 0.25m)
            weakMetrics.Add("repeated plan overspending");
        if (stability.Metrics.IncomeStability < 0.6m)
            weakMetrics.Add("unstable income");
        if (stability.Metrics.GoalAchievementIndex < 0.7m)
            weakMetrics.Add("budget limits missed against goals");

        var prefix = delta < 0
            ? $"The stability index declined by {Math.Abs(delta)} points"
            : "The stability index is driven";
        return weakMetrics.Count == 0
            ? $"{prefix}, but no single weak metric dominates."
            : $"{prefix} because of {string.Join(", ", weakMetrics)}.";
    }

    private static string BuildBehaviorWhy(BehavioralScoreDto behavior, int delta)
    {
        var weakMetrics = new List<string>();
        if (behavior.Metrics.LimitAdherence < 0.75m)
            weakMetrics.Add("budget limits are exceeded");
        if (behavior.Metrics.ImpulseControl < 0.75m)
            weakMetrics.Add("unplanned or unusually large expenses");
        if (behavior.Metrics.SavingsRegularity < 0.67m)
            weakMetrics.Add("irregular savings rhythm");
        if (behavior.Metrics.WarningResponse < 0.7m)
            weakMetrics.Add("slow reaction to overspending warnings");

        var prefix = delta < 0
            ? $"The behavioral score declined by {Math.Abs(delta)} points"
            : "The behavioral score is driven";
        return weakMetrics.Count == 0
            ? $"{prefix}, with no major behavioral weakness detected."
            : $"{prefix} because {string.Join(", ", weakMetrics)}.";
    }

    private static string MapRecommendationToAction(string recommendation)
    {
        var text = recommendation.ToLowerInvariant();
        if (text.Contains("saving") || text.Contains("save") || text.Contains("savings"))
            return "Create or increase a recurring transfer to a savings account after income arrives.";
        if (text.Contains("budget") || text.Contains("limit") || text.Contains("overspending"))
            return "Open the current plan, identify categories above limit, and apply corrections before creating the next plan.";
        if (text.Contains("income"))
            return "Use conservative income in the next auto plan and avoid increasing flexible limits until income stabilizes.";
        if (text.Contains("emergency"))
            return "Move a fixed amount monthly to a dedicated savings account until it covers at least 3 months of expenses.";
        if (text.Contains("fixed") || text.Contains("regular"))
            return "Review recurring payments and reduce or pause non-essential obligations.";

        return "Review the related page and apply the smallest change that removes the weak metric.";
    }

    private static string ResolveOverallStatus(
        FinancialStabilityDto stability,
        int stabilityDelta,
        BehavioralScoreDto behavior,
        int behaviorDelta,
        List<DashboardFinancialGoalDto> goals)
    {
        if (stability.Index < 40 || behavior.Score < 40 || stabilityDelta <= -10 || behaviorDelta <= -10)
            return "High attention";
        if (stability.Index < 70 || behavior.Score < 70 || goals.Any(g => g.IsOffTrack || !g.IsAchievable))
            return "Needs attention";
        return "Healthy";
    }

    private static string ResolveStabilityPriority(FinancialStabilityDto stability, int delta)
    {
        if (stability.Index < 40 || delta <= -10)
            return "High";
        if (stability.Index < 70 || delta <= -5)
            return "Medium";
        return "Low";
    }

    private static string ResolveBehaviorPriority(BehavioralScoreDto behavior, int delta)
    {
        if (behavior.Score < 40 || delta <= -10)
            return "High";
        if (behavior.Score < 70 || delta <= -5)
            return "Medium";
        return "Low";
    }

    private static int PriorityRank(string priority)
    {
        return priority switch
        {
            "High" => 0,
            "Medium" => 1,
            _ => 2
        };
    }

    private static string FormatPercent(decimal value)
    {
        return $"{Math.Round(value * 100m, 1)}%";
    }

    private static decimal ParsePercent(string value)
    {
        return decimal.TryParse(value.Replace("%", string.Empty), out var parsed)
            ? parsed
            : 0m;
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

    public async Task<BudgetPlanPageDto> GetBudgetPlanPageAsync(int planId, bool includeEvents, CancellationToken ct)
    {
        var plan = await _budgetPlanManager.GetByIdAsync(planId, ct);
        if (plan == null)
            throw new CustomException($"Budget plan {planId} not found", StatusCodes.Status404NotFound, "budget_plan_not_found");

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
        var forecast = await BuildMonthEndForecastAsync(plan, items.ToList(), transactions, plannedCategoryIds, ct);
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
            MonthEndForecast = forecast.Total
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
            if (forecast.ByCategory.TryGetValue(item.CategoryId, out var itemForecast))
            {
                itemDto.ProjectedSpent = itemForecast.ProjectedTotalSpent;
                itemDto.ProjectedRemaining = item.Amount - itemForecast.ProjectedTotalSpent;
                itemDto.ProjectedVariableSpending = itemForecast.ProjectedVariableSpending;
                itemDto.FutureRecurringSpending = itemForecast.FutureRecurringSpending;
            }
            else
            {
                itemDto.ProjectedSpent = spent;
                itemDto.ProjectedRemaining = item.Amount - spent;
            }
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
                ProjectedSpent = forecast.Other.ProjectedTotalSpent,
                ProjectedRemaining = -forecast.Other.ProjectedTotalSpent,
                ProjectedVariableSpending = forecast.Other.ProjectedVariableSpending,
                FutureRecurringSpending = forecast.Other.FutureRecurringSpending,
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

    private async Task<BudgetForecastCalculation> BuildMonthEndForecastAsync(
        BudgetPlan plan,
        List<BudgetPlanItem> items,
        List<Transaction> planTransactions,
        HashSet<int> plannedCategoryIds,
        CancellationToken ct)
    {
        if (plan.Type != BudgetPlanType.Monthly)
            return BudgetForecastCalculation.Empty(plan);

        var start = plan.StartDate.Date;
        var endExclusive = plan.EndDate.Date.AddDays(1);
        var todayExclusive = DateTime.UtcNow.Date.AddDays(1);
        var cutoffExclusive = todayExclusive < start
            ? start
            : todayExclusive > endExclusive
                ? endExclusive
                : todayExclusive;
        var elapsedDays = Math.Max(0, (cutoffExclusive - start).Days);
        var remainingDays = Math.Max(0, (endExclusive - cutoffExclusive).Days);

        var recurringPayments = await _ctx.RecurringPayments
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.Type == TransactionCategoryType.Expense
                && p.StartDate < endExclusive
                && (p.EndDate == null || p.EndDate >= start))
            .ToListAsync(ct);

        var recurringElapsedByCategory = recurringPayments
            .SelectMany(payment => RecurringPaymentSchedule.GetOccurrences(payment, start, cutoffExclusive)
                .Select(date => new { payment.CategoryId, payment.Amount, Date = date }))
            .Where(o => o.CategoryId.HasValue)
            .GroupBy(o => o.CategoryId)
            .ToDictionary(g => g.Key!.Value, g => g.Sum(o => o.Amount));
        var recurringElapsedOther = recurringPayments
            .Where(payment => !payment.CategoryId.HasValue)
            .SelectMany(payment => RecurringPaymentSchedule.GetOccurrences(payment, start, cutoffExclusive)
                .Select(_ => payment.Amount))
            .Sum();

        var recurringFutureByCategory = recurringPayments
            .SelectMany(payment => RecurringPaymentSchedule.GetOccurrences(payment, cutoffExclusive, endExclusive)
                .Select(date => new { payment.CategoryId, payment.Amount, Date = date }))
            .Where(o => o.CategoryId.HasValue)
            .GroupBy(o => o.CategoryId)
            .ToDictionary(g => g.Key!.Value, g => g.Sum(o => o.Amount));
        var recurringFutureOther = recurringPayments
            .Where(payment => !payment.CategoryId.HasValue)
            .SelectMany(payment => RecurringPaymentSchedule.GetOccurrences(payment, cutoffExclusive, endExclusive)
                .Select(_ => payment.Amount))
            .Sum();

        var actualSpentByCategory = planTransactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date < cutoffExclusive
                && t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key!.Value, g => g.Sum(t => t.Amount));
        var actualOther = planTransactions
            .Where(t => t.Type == TransactionCategoryType.Expense
                && t.Date < cutoffExclusive
                && !t.CategoryId.HasValue)
            .Sum(t => t.Amount);

        var categoryIds = items
            .Select(i => i.CategoryId)
            .Concat(actualSpentByCategory.Keys)
            .Concat(recurringFutureByCategory.Keys)
            .Distinct()
            .ToList();
        var resultByCategory = new Dictionary<int, CategoryForecastDto>();
        var other = new CategoryForecastDto();

        foreach (var categoryId in categoryIds)
        {
            var actualSpent = actualSpentByCategory.GetValueOrDefault(categoryId);
            var elapsedRecurring = recurringElapsedByCategory.GetValueOrDefault(categoryId);
            var coveredRecurring = Math.Min(actualSpent, elapsedRecurring);
            var variableSpent = Math.Max(0m, actualSpent - coveredRecurring);
            var projectedVariable = elapsedDays > 0 && remainingDays > 0
                ? Math.Round(variableSpent / elapsedDays * remainingDays, 2)
                : 0m;
            var futureRecurring = recurringFutureByCategory.GetValueOrDefault(categoryId);
            var forecast = new CategoryForecastDto
            {
                ActualSpent = Math.Round(actualSpent, 2),
                ProjectedVariableSpending = projectedVariable,
                FutureRecurringSpending = Math.Round(futureRecurring, 2),
                ProjectedTotalSpent = Math.Round(actualSpent + projectedVariable + futureRecurring, 2)
            };

            if (plannedCategoryIds.Contains(categoryId))
                resultByCategory[categoryId] = forecast;
            else
                other = SumForecast(other, forecast);
        }

        if (actualOther > 0m || recurringFutureOther > 0m)
        {
            var coveredRecurring = Math.Min(actualOther, recurringElapsedOther);
            var variableSpent = Math.Max(0m, actualOther - coveredRecurring);
            var projectedVariable = elapsedDays > 0 && remainingDays > 0
                ? Math.Round(variableSpent / elapsedDays * remainingDays, 2)
                : 0m;
            other = SumForecast(other, new CategoryForecastDto
            {
                ActualSpent = Math.Round(actualOther, 2),
                ProjectedVariableSpending = projectedVariable,
                FutureRecurringSpending = Math.Round(recurringFutureOther, 2),
                ProjectedTotalSpent = Math.Round(actualOther + projectedVariable + recurringFutureOther, 2)
            });
        }

        var totalActualSpent = Math.Round(planTransactions
            .Where(t => t.Type == TransactionCategoryType.Expense && t.Date < cutoffExclusive)
            .Sum(t => t.Amount), 2);
        var totalProjectedVariable = Math.Round(resultByCategory.Values.Sum(f => f.ProjectedVariableSpending)
            + other.ProjectedVariableSpending, 2);
        var totalFutureRecurring = Math.Round(resultByCategory.Values.Sum(f => f.FutureRecurringSpending)
            + other.FutureRecurringSpending, 2);
        var totalProjected = Math.Round(totalActualSpent + totalProjectedVariable + totalFutureRecurring, 2);
        var budgetLimit = Math.Round(items.Sum(i => i.Amount), 2);

        return new BudgetForecastCalculation
        {
            ByCategory = resultByCategory,
            Other = other,
            Total = new MonthEndForecastDto
            {
                AsOfDate = cutoffExclusive.AddDays(-1),
                PeriodEnd = plan.EndDate.Date,
                ElapsedDays = elapsedDays,
                RemainingDays = remainingDays,
                ActualSpent = totalActualSpent,
                ProjectedVariableSpending = totalProjectedVariable,
                FutureRecurringSpending = totalFutureRecurring,
                ProjectedTotalSpent = totalProjected,
                BudgetLimit = budgetLimit,
                ProjectedRemaining = Math.Round(budgetLimit - totalProjected, 2),
                Status = totalProjected > budgetLimit ? "Over limit" : "On track"
            }
        };
    }

    private static CategoryForecastDto SumForecast(CategoryForecastDto left, CategoryForecastDto right)
    {
        return new CategoryForecastDto
        {
            ActualSpent = Math.Round(left.ActualSpent + right.ActualSpent, 2),
            ProjectedVariableSpending = Math.Round(left.ProjectedVariableSpending + right.ProjectedVariableSpending, 2),
            FutureRecurringSpending = Math.Round(left.FutureRecurringSpending + right.FutureRecurringSpending, 2),
            ProjectedTotalSpent = Math.Round(left.ProjectedTotalSpent + right.ProjectedTotalSpent, 2)
        };
    }

    private sealed class BudgetForecastCalculation
    {
        public MonthEndForecastDto? Total { get; set; }
        public Dictionary<int, CategoryForecastDto> ByCategory { get; set; } = new();
        public CategoryForecastDto Other { get; set; } = new();

        public static BudgetForecastCalculation Empty(BudgetPlan plan)
        {
            return new BudgetForecastCalculation
            {
                Total = new MonthEndForecastDto
                {
                    AsOfDate = DateTime.UtcNow.Date,
                    PeriodEnd = plan.EndDate.Date,
                    BudgetLimit = plan.Items?.Sum(i => i.Amount) ?? 0m
                }
            };
        }
    }

    private sealed class CategoryForecastDto
    {
        public decimal ActualSpent { get; set; }
        public decimal ProjectedVariableSpending { get; set; }
        public decimal FutureRecurringSpending { get; set; }
        public decimal ProjectedTotalSpent { get; set; }
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
            throw new CustomException($"Event {eventId} not found", StatusCodes.Status404NotFound, "event_not_found");

        return await GetBudgetPlanPageAsync(eventId, false, ct);
    }

    public async Task<IncomesByMonthDto> GetIncomesByMonthAsync(int month, int? year, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new CustomException("Month must be between 1 and 12", StatusCodes.Status400BadRequest, "invalid_month");

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
            throw new CustomException("Month must be between 1 and 12", StatusCodes.Status400BadRequest, "invalid_month");

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
            throw new CustomException("Month must be between 1 and 12", StatusCodes.Status400BadRequest, "invalid_month");

        var yr = year ?? DateTime.Today.Year;
        var start = new DateTime(yr, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var tx = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.Type == TransactionCategoryType.Transfer && t.Date >= start && t.Date < end)
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
            throw new CustomException("Month must be between 1 and 12", StatusCodes.Status400BadRequest, "invalid_month");

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
            .GroupBy(t => new { t.Category!.Title, t.Category.Color })
            .Select(g => new { Label = g.Key.Title, g.Key.Color, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .ToList();
        var expCatTotal = topExpCats.Sum(g => g.Amount);
        var topExpDtos = topExpCats
            .Select(g => new LabelAmountPercentDto
            {
                Label = g.Label,
                Amount = g.Amount,
                Percent = expCatTotal == 0 ? "0%" : $"{Math.Round(g.Amount / expCatTotal * 100, 2)}%",
                Color = g.Color
            }).ToList();

        var topIncCats = tx
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => new { t.Category!.Title, t.Category.Color })
            .Select(g => new { Label = g.Key.Title, g.Key.Color, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .ToList();
        var incCatTotal = topIncCats.Sum(g => g.Amount);
        var topIncDtos = topIncCats
            .Select(g => new LabelAmountPercentDto
            {
                Label = g.Label,
                Amount = g.Amount,
                Percent = incCatTotal == 0 ? "0%" : $"{Math.Round(g.Amount / incCatTotal * 100, 2)}%",
                Color = g.Color
            }).ToList();

        var expByCat = tx
            .Where(t => t.Type == TransactionCategoryType.Expense && t.CategoryId != null)
            .GroupBy(t => new { t.Category!.Title, t.Category.Color })
            .Select(g => new LabelValueDto { Label = g.Key.Title, Value = g.Sum(x => x.Amount), Color = g.Key.Color })
            .ToList();

        var incByCat = tx
            .Where(t => t.Type == TransactionCategoryType.Income && t.CategoryId != null)
            .GroupBy(t => new { t.Category!.Title, t.Category.Color })
            .Select(g => new LabelValueDto { Label = g.Key.Title, Value = g.Sum(x => x.Amount), Color = g.Key.Color })
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

