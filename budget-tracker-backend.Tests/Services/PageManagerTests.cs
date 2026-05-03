using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class PageManagerTests
{
    private static PageManager CreateManager(ApplicationDbContext context)
    {
        var mapper = TestInfrastructure.CreateMapper();
        var accountManager = new AccountManager(context, mapper);
        var budgetPlanManager = new BudgetPlanManager(context, mapper);
        var budgetPlanItemManager = new BudgetPlanItemManager(context, mapper);
        var transactionManager = new TransactionManager(context, mapper, accountManager);
        var financialGoalManager = new FinancialGoalManager(context, mapper);

        return new PageManager(
            context,
            mapper,
            accountManager,
            budgetPlanManager,
            budgetPlanItemManager,
            transactionManager,
            financialGoalManager);
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

        Assert.That(result.FinancialStability.Metrics.GoalAchievementIndex, Is.LessThan(1m));
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
    public async Task GetEventPageAsync_WhenPlanIsNotEvent_ThrowsException()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        Assert.That(async () => await manager.GetEventPageAsync(1, CancellationToken.None),
            Throws.TypeOf<Exception>().With.Message.EqualTo("Event 1 not found"));
    }

    [TestCase(0)]
    [TestCase(13)]
    public void MonthlyPageMethods_WhenMonthOutOfRange_ThrowException(int month)
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = CreateManager(context);

        Assert.Multiple(() =>
        {
            Assert.That(async () => await manager.GetIncomesByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<Exception>());
            Assert.That(async () => await manager.GetExpensesByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<Exception>());
            Assert.That(async () => await manager.GetTransfersByMonthAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<Exception>());
            Assert.That(async () => await manager.GetMonthlyReportAsync(month, TestInfrastructure.CurrentMonthStart.Year, CancellationToken.None), Throws.TypeOf<Exception>());
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
