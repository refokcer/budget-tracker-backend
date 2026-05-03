using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class BudgetPlanManagerTests
{
    private static BudgetPlanManager CreateManager(ApplicationDbContext context)
    {
        return new BudgetPlanManager(context, TestInfrastructure.CreateMapper());
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_CarriesRemainingBudgetIntoLowerNextLimit()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);
        var targetStart = TestInfrastructure.CurrentMonthStart.AddMonths(1);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = targetStart.Month,
            Year = targetStart.Year
        }, CancellationToken.None);

        var groceries = result.Items.Single(i => i.CategoryTitle == "Groceries");

        Assert.Multiple(() =>
        {
            Assert.That(result.SourcePlanId, Is.EqualTo(1));
            Assert.That(result.Plan.StartDate, Is.EqualTo(targetStart));
            Assert.That(groceries.PreviousLimit, Is.EqualTo(300m));
            Assert.That(groceries.SpentAmount, Is.EqualTo(120m));
            Assert.That(groceries.RemainingAmount, Is.EqualTo(180m));
            Assert.That(groceries.CarryAdjustment, Is.EqualTo(-45m));
            Assert.That(groceries.RecommendedAmount, Is.EqualTo(255m));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_AddsPartialOverspendToNextLimit()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Extra groceries",
            Amount = 240m,
            CategoryId = 2,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(18),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "auto-plan-extra-groceries"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);
        var targetStart = TestInfrastructure.CurrentMonthStart.AddMonths(1);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = targetStart.Month,
            Year = targetStart.Year
        }, CancellationToken.None);

        var groceries = result.Items.Single(i => i.CategoryTitle == "Groceries");

        Assert.Multiple(() =>
        {
            Assert.That(groceries.SpentAmount, Is.EqualTo(360m));
            Assert.That(groceries.OverspentAmount, Is.EqualTo(60m));
            Assert.That(groceries.CarryAdjustment, Is.EqualTo(30m));
            Assert.That(groceries.RecommendedAmount, Is.EqualTo(330m));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_AppliesWinterUtilitySeasonality()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Utilities",
            Type = TransactionCategoryType.Expense,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlans.AddAsync(new BudgetPlan
        {
            Id = 10,
            Title = "November Plan",
            StartDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc),
            Type = BudgetPlanType.Monthly,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlanItems.AddAsync(new BudgetPlanItem
        {
            Id = 10,
            BudgetPlanId = 10,
            CategoryId = 10,
            Amount = 100m,
            CurrencyId = 1
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "November utilities",
            Amount = 100m,
            CategoryId = 10,
            CurrencyId = 1,
            BudgetPlanId = 10,
            Date = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "november-utilities"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = 12,
            Year = 2026
        }, CancellationToken.None);

        var utilities = result.Items.Single(i => i.CategoryTitle == "Utilities");

        Assert.Multiple(() =>
        {
            Assert.That(utilities.SeasonalityMultiplier, Is.EqualTo(1.25m));
            Assert.That(utilities.RecommendedAmount, Is.EqualTo(125m));
            Assert.That(utilities.Description, Is.EqualTo("Higher because of winter utilities."));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_ReducesUtilitiesWhenWinterEnds()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Utilities",
            Type = TransactionCategoryType.Expense,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlans.AddAsync(new BudgetPlan
        {
            Id = 10,
            Title = "February Plan",
            StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
            Type = BudgetPlanType.Monthly,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlanItems.AddAsync(new BudgetPlanItem
        {
            Id = 10,
            BudgetPlanId = 10,
            CategoryId = 10,
            Amount = 125m,
            CurrencyId = 1
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "February utilities",
            Amount = 125m,
            CategoryId = 10,
            CurrencyId = 1,
            BudgetPlanId = 10,
            Date = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "february-utilities"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = 3,
            Year = 2026
        }, CancellationToken.None);

        var utilities = result.Items.Single(i => i.CategoryTitle == "Utilities");

        Assert.Multiple(() =>
        {
            Assert.That(utilities.SeasonalityMultiplier, Is.EqualTo(0.8m));
            Assert.That(utilities.RecommendedAmount, Is.EqualTo(100m));
            Assert.That(utilities.Description, Is.EqualTo("Lower because winter season ended."));
        });
    }
}
