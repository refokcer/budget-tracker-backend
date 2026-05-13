using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Tests.Common;
using Microsoft.AspNetCore.Http;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class PageManagerTests
{
    private static PageManager CreateManager(ApplicationDbContext context)
    {
        var mapper = TestInfrastructure.CreateMapper();
        var accountManager = new AccountManager(context, mapper);
        var budgetPlanManager = TestInfrastructure.CreateBudgetPlanManager(context, mapper);
        var budgetPlanItemManager = new BudgetPlanItemManager(context, mapper);
        var transactionManager = new TransactionManager(context, mapper, accountManager);
        var financialGoalManager = TestInfrastructure.CreateFinancialGoalManager(context, mapper);

        return new PageManager(
            context,
            mapper,
            accountManager,
            budgetPlanManager,
            budgetPlanItemManager,
            transactionManager,
            financialGoalManager,
            new FinancialStabilityAlgorithm(context),
            new BehavioralScoreAlgorithm(context));
    }

    [Test]
    public async Task GetDashboardAsync_ReturnsBalanceTopCategoriesAndLargestTransaction()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.GetDashboardAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalBalance, Is.EqualTo(1700m));
            Assert.That(result.TopExpenses.First().CategoryTitle, Is.EqualTo("Groceries"));
            Assert.That(result.TopExpenses.First().Amount, Is.EqualTo(160m));
            Assert.That(result.TopIncomes.First().Amount, Is.EqualTo(2500m));
            Assert.That(result.BiggestTransaction!.Title, Is.EqualTo("Salary payment"));
            Assert.That(result.FinancialStability.Index, Is.InRange(0, 100));
            Assert.That(result.FinancialStability.Level, Is.Not.Empty);
            Assert.That(result.FinancialStability.Metrics.GoalAchievementIndex, Is.InRange(0m, 1m));
            Assert.That(result.FinancialStability.Recommendations, Is.Not.Empty);
            Assert.That(result.BehavioralScore.Score, Is.InRange(0, 100));
            Assert.That(result.BehavioralScore.Level, Is.Not.Empty);
            Assert.That(result.BehavioralScore.Metrics.LimitAdherence, Is.InRange(0m, 1m));
            Assert.That(result.BehavioralScore.Metrics.ImpulseControl, Is.InRange(0m, 1m));
            Assert.That(result.BehavioralScore.Metrics.SavingsRegularity, Is.InRange(0m, 1m));
            Assert.That(result.BehavioralScore.Metrics.WarningResponse, Is.InRange(0m, 1m));
            Assert.That(result.BehavioralScore.Insights, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetDashboardAsync_ReducesGoalAchievementIndexWhenCategoryBudgetIsOverspent()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Extra groceries",
            Amount = 480m,
            CategoryId = 2,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(12),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "extra-groceries"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.GetDashboardAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinancialStability.Metrics.GoalAchievementIndex, Is.LessThan(1m));
            Assert.That(result.BehavioralScore.Metrics.LimitAdherence, Is.LessThan(1m));
            Assert.That(result.BehavioralScore.Metrics.OverspentCategories, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task GetDashboardAsync_MarksLargeUnplannedExpenseAsImpulsiveBehavior()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Gadgets",
            Type = TransactionCategoryType.Expense,
            UserId = TestInfrastructure.UserId
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Impulse headphones",
            Amount = 700m,
            CategoryId = 10,
            CurrencyId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(18),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "impulse-headphones"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.GetDashboardAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.BehavioralScore.Metrics.ImpulsiveTransactions, Is.GreaterThan(0));
            Assert.That(result.BehavioralScore.Metrics.ImpulseControl, Is.LessThan(1m));
            Assert.That(result.BehavioralScore.Metrics.ImpulsiveAmountShare, Is.GreaterThan(0m));
        });
    }

    [Test]
    public async Task GetDashboardAsync_UsesAccountTypeToDetectSavingsAccounts()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);

        var savingsAccount = await context.Accounts.FindAsync(2);
        Assert.That(savingsAccount, Is.Not.Null);
        savingsAccount!.Title = "Family Buffer";

        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Large monthly expense",
            Amount = 2400m,
            CategoryId = 2,
            CurrencyId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(15),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "large-monthly-expense"
        });

        await context.SaveChangesAsync(CancellationToken.None);

        var manager = CreateManager(context);
        var result = await manager.GetDashboardAsync(CancellationToken.None);

        Assert.That(result.FinancialStability.Metrics.SavingsShare, Is.EqualTo(0.12m));
    }

    [Test]
    public async Task GetDashboardAsync_IncludesFinancialGoalsSummary()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);

        await context.FinancialGoals.AddAsync(new FinancialGoal
        {
            Title = "Vacation",
            TargetAmount = 1000m,
            InitialAmount = 100m,
            TargetDate = DateTime.UtcNow.AddMonths(4),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            Description = "Summer vacation",
            UserId = TestInfrastructure.UserId
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var manager = CreateManager(context);
        var result = await manager.GetDashboardAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.FinancialGoals, Has.Count.EqualTo(1));
            Assert.That(result.FinancialGoals[0].Title, Is.EqualTo("Vacation"));
            Assert.That(result.FinancialGoals[0].CurrentSavedAmount, Is.GreaterThan(0m));
            Assert.That(result.FinancialGoals[0].RiskLevel, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetFinancialRecommendationsAsync_ReturnsSignalsAndActions()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.GetFinancialRecommendationsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Headline, Is.Not.Empty);
            Assert.That(result.Explanation, Is.Not.Empty);
            Assert.That(result.FinancialStabilityIndex, Is.InRange(0, 100));
            Assert.That(result.BehavioralScore, Is.InRange(0, 100));
            Assert.That(result.Signals, Is.Not.Empty);
            Assert.That(result.PriorityActions, Is.Not.Empty);
            Assert.That(result.Sections.Select(s => s.Title), Does.Contain("Financial stability"));
            Assert.That(result.Sections.Select(s => s.Title), Does.Contain("Financial behavior"));
        });
    }

    [Test]
    public async Task GetBudgetPlanPageAsync_WhenIncludingEvents_AddsEventSummariesAndTransactions()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.GetBudgetPlanPageAsync(1, includeEvents: true, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Plan.Title, Is.EqualTo("March Plan"));
            Assert.That(result.Items.Select(i => i.CategoryTitle), Does.Contain("Groceries"));
            Assert.That(result.Items.Select(i => i.CategoryTitle), Does.Contain("Birthday Event"));
            Assert.That(result.Items.First(i => i.CategoryTitle == "Groceries").Spent, Is.EqualTo(120m));
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].Plan.Title, Is.EqualTo("Birthday Event"));
            Assert.That(result.Events[0].Items.First().Remaining, Is.EqualTo(110m));
        });
    }

    [Test]
    public async Task GetBudgetPlanPageAsync_GroupsUnplannedCategoryExpensesIntoOtherRow()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);

        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Entertainment",
            Type = TransactionCategoryType.Expense,
            UserId = TestInfrastructure.UserId
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Id = 20,
            Title = "Movie tickets",
            Amount = 75m,
            CategoryId = 10,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(14),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "movie-tickets"
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var manager = CreateManager(context);
        var result = await manager.GetBudgetPlanPageAsync(1, includeEvents: false, CancellationToken.None);
        var other = result.Items.Single(i => i.IsOther);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.First(i => i.CategoryTitle == "Groceries").Spent, Is.EqualTo(120m));
            Assert.That(other.CategoryTitle, Is.EqualTo("Other"));
            Assert.That(other.IsVirtual, Is.True);
            Assert.That(other.Amount, Is.EqualTo(0m));
            Assert.That(other.Spent, Is.EqualTo(75m));
            Assert.That(other.Remaining, Is.EqualTo(-75m));
        });
    }

    [Test]
    public async Task GetBudgetPlanPageAsync_WhenOtherBudgetExists_UsesItAsUnplannedCategoryLimit()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);

        await context.Categories.AddRangeAsync(
            new Category
            {
                Id = 10,
                Title = "Entertainment",
                Type = TransactionCategoryType.Expense,
                UserId = TestInfrastructure.UserId
            },
            new Category
            {
                Id = 11,
                Title = "Other",
                Type = TransactionCategoryType.Expense,
                UserId = TestInfrastructure.UserId
            });
        await context.BudgetPlanItems.AddAsync(new BudgetPlanItem
        {
            Id = 12,
            BudgetPlanId = 1,
            CategoryId = 11,
            Amount = 100m,
            CurrencyId = 1,
            Description = "Flexible category bucket"
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Id = 20,
            Title = "Movie tickets",
            Amount = 75m,
            CategoryId = 10,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(14),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "movie-tickets"
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var manager = CreateManager(context);
        var result = await manager.GetBudgetPlanPageAsync(1, includeEvents: false, CancellationToken.None);
        var other = result.Items.Single(i => i.IsOther);

        Assert.Multiple(() =>
        {
            Assert.That(other.Id, Is.EqualTo(12));
            Assert.That(other.IsVirtual, Is.False);
            Assert.That(other.Amount, Is.EqualTo(100m));
            Assert.That(other.Spent, Is.EqualTo(75m));
            Assert.That(other.Remaining, Is.EqualTo(25m));
        });
    }

    [Test]
    public async Task GetBudgetPlanPageAsync_ForecastDoesNotTreatElapsedRecurringPaymentsAsDailyPace()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);

        var today = DateTime.UtcNow.Date;
        await context.RecurringPayments.AddAsync(new RecurringPayment
        {
            Title = "Large utility subscription",
            Amount = 10000m,
            CurrencyId = 1,
            CategoryId = 2,
            AccountFrom = 1,
            Type = TransactionCategoryType.Expense,
            Frequency = RecurringPaymentFrequency.Monthly,
            DayOfMonth = today.Day,
            StartDate = TestInfrastructure.CurrentMonthStart,
            IsActive = true,
            UserId = TestInfrastructure.UserId
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Large utility subscription",
            Amount = 10000m,
            CategoryId = 2,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = today,
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "large-recurring-expense",
            AuthCode = $"recurring:900:{today:yyyyMMdd}"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.GetBudgetPlanPageAsync(1, includeEvents: false, CancellationToken.None);
        var forecast = result.MonthEndForecast!;
        var groceries = result.Items.First(i => i.CategoryTitle == "Groceries");

        Assert.Multiple(() =>
        {
            Assert.That(forecast.ActualSpent, Is.EqualTo(10120m));
            Assert.That(forecast.ProjectedVariableSpending, Is.LessThan(1000m));
            Assert.That(forecast.ProjectedTotalSpent, Is.LessThan(12000m));
            Assert.That(groceries.FutureRecurringSpending, Is.EqualTo(0m));
            Assert.That(groceries.ProjectedSpent, Is.EqualTo(forecast.ProjectedTotalSpent));
        });
    }

    [Test]
    public async Task GetEventPageAsync_WhenPlanIsNotEvent_ThrowsException()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        Assert.That(async () => await manager.GetEventPageAsync(1, CancellationToken.None),
            Throws.TypeOf<CustomException>()
                .With.Message.EqualTo("Event 1 not found")
                .And.Property(nameof(CustomException.Code)).EqualTo("event_not_found")
                .And.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status404NotFound));
    }

    [TestCase(0)]
    [TestCase(13)]
    public void MonthlyPageMethods_WhenMonthOutOfRange_ThrowException(int month)
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = CreateManager(context);

        Assert.Multiple(() =>
        {
            Assert.That(async () => await manager.GetIncomesByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<CustomException>());
            Assert.That(async () => await manager.GetExpensesByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<CustomException>());
            Assert.That(async () => await manager.GetTransfersByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<CustomException>());
            Assert.That(async () => await manager.GetMonthlyReportAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<CustomException>());
        });
    }

    [Test]
    public async Task GetIncomesExpensesTransfersByMonthAsync_ReturnOnlyTransactionsForRequestedMonthAndType()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var incomes = await manager.GetIncomesByMonthAsync(TestInfrastructure.CurrentMonthStart.Month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None);
        var expenses = await manager.GetExpensesByMonthAsync(TestInfrastructure.CurrentMonthStart.Month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None);
        var transfers = await manager.GetTransfersByMonthAsync(TestInfrastructure.CurrentMonthStart.Month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(incomes.Transactions.Select(t => t.Title), Is.EquivalentTo(new[] { "Salary payment" }));
            Assert.That(expenses.Transactions.Select(t => t.Title), Is.EquivalentTo(new[] { "Weekly groceries", "Event groceries" }));
            Assert.That(transfers.Transactions.Select(t => t.Title), Is.EquivalentTo(new[] { "Move to savings" }));
        });
    }

    [Test]
    public async Task GetMonthlyReportAsync_AggregatesTotalsCategoriesAccountsAndTopExpense()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var report = await manager.GetMonthlyReportAsync(TestInfrastructure.CurrentMonthStart.Month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(report.TotalExp, Is.EqualTo(160m));
            Assert.That(report.TotalInc, Is.EqualTo(2500m));
            Assert.That(report.Balance, Is.EqualTo(2340m));
            Assert.That(report.DefaultCurrency, Is.EqualTo("USD"));
            Assert.That(report.TopExpenseCategories.First().Percent, Is.EqualTo("100%"));
            Assert.That(report.ExpensesByAccount.Single().Label, Is.EqualTo("Cash"));
            Assert.That(report.TopExpenseTransaction!.Title, Is.EqualTo("Weekly groceries"));
        });
    }
}
