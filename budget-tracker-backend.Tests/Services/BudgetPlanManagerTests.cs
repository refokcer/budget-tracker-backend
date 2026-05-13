using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class BudgetPlanManagerTests
{
    private static BudgetPlanManager CreateManager(ApplicationDbContext context)
    {
        var mapper = TestInfrastructure.CreateMapper();
        return TestInfrastructure.CreateBudgetPlanManager(context, mapper);
    }

    private static async Task SaveAutoRulesAsync(
        ApplicationDbContext context,
        budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDto rules)
    {
        await context.UserClaims.AddAsync(new Microsoft.AspNetCore.Identity.IdentityUserClaim<string>
        {
            UserId = TestInfrastructure.UserId,
            ClaimType = budget_tracker_backend.Services.UserSettings.UserSettingsClaimTypes.AutoBudgetPlanRules,
            ClaimValue = System.Text.Json.JsonSerializer.Serialize(
                rules,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
        });
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
            Assert.That(groceries.HistoryAverageAmount, Is.EqualTo(95m));
            Assert.That(groceries.Priority, Is.EqualTo("Essential"));
            Assert.That(groceries.RecommendedAmount, Is.EqualTo(215m));
            Assert.That(result.AverageMonthlyIncome, Is.EqualTo(2500m));
            Assert.That(result.IncomeEnvelopeMultiplier, Is.EqualTo(1m));
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
            Assert.That(groceries.HistoryAverageAmount, Is.EqualTo(215m));
            Assert.That(groceries.RecommendedAmount, Is.EqualTo(301.25m));
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
            Priority = CategoryPriority.Mandatory,
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
        var rules = new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDto
        {
            CategoryRules =
            [
                new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanCategoryRuleDto
                {
                    CategoryId = 10,
                    MonthCoefficients = budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDefaults.CreateMonthCoefficients()
                }
            ]
        };
        rules.CategoryRules[0].MonthCoefficients.Single(i => i.Month == 12).Multiplier = 1.25m;
        await SaveAutoRulesAsync(context, rules);
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
            Assert.That(utilities.Description, Is.EqualTo("Higher because of configured month coefficient."));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_AppliesConfiguredRules()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var targetStart = TestInfrastructure.CurrentMonthStart.AddMonths(1);
        var rules = new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDto
        {
            CategoryRules =
            [
                new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanCategoryRuleDto
                {
                    CategoryId = 2,
                    CutBehavior = "Protected",
                    MonthCoefficients = budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDefaults.CreateMonthCoefficients()
                }
            ]
        };
        rules.CategoryRules[0].MonthCoefficients.Single(i => i.Month == targetStart.Month).Multiplier = 1.07m;
        await SaveAutoRulesAsync(context, rules);
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = targetStart.Month,
            Year = targetStart.Year
        }, CancellationToken.None);

        var groceries = result.Items.Single(i => i.CategoryTitle == "Groceries");

        Assert.Multiple(() =>
        {
            Assert.That(groceries.CarryAdjustment, Is.EqualTo(0m));
            Assert.That(groceries.SeasonalityMultiplier, Is.EqualTo(1.07m));
            Assert.That(groceries.RecommendedAmount, Is.EqualTo(300m));
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
            Priority = CategoryPriority.Mandatory,
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
        var rules = new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDto
        {
            CategoryRules =
            [
                new budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanCategoryRuleDto
                {
                    CategoryId = 10,
                    MonthCoefficients = budget_tracker_backend.Dto.UserSettings.AutoBudgetPlanRulesDefaults.CreateMonthCoefficients()
                }
            ]
        };
        rules.CategoryRules[0].MonthCoefficients.Single(i => i.Month == 2).Multiplier = 1.25m;
        rules.CategoryRules[0].MonthCoefficients.Single(i => i.Month == 3).Multiplier = 1m;
        await SaveAutoRulesAsync(context, rules);
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
            Assert.That(utilities.Description, Is.EqualTo("Lower because of configured month coefficient."));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_WhenFinancialStateDeclines_CutsDiscretionaryCategoriesMore()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Entertainment",
            Type = TransactionCategoryType.Expense,
            Priority = CategoryPriority.Discretionary,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlanItems.AddAsync(new BudgetPlanItem
        {
            Id = 10,
            BudgetPlanId = 1,
            CategoryId = 10,
            Amount = 1000m,
            CurrencyId = 1
        });
        await context.Transactions.AddAsync(new Transaction
        {
            Title = "Big entertainment month",
            Amount = 1000m,
            CategoryId = 10,
            CurrencyId = 1,
            BudgetPlanId = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(18),
            Type = TransactionCategoryType.Expense,
            AccountFrom = 1,
            UserId = TestInfrastructure.UserId,
            UnicCode = "big-entertainment-month"
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
        var entertainment = result.Items.Single(i => i.CategoryTitle == "Entertainment");

        Assert.Multiple(() =>
        {
            Assert.That(result.FinancialStateTrend, Is.LessThan(0m));
            Assert.That(entertainment.Priority, Is.EqualTo("Discretionary"));
            Assert.That(entertainment.FinancialStateMultiplier, Is.LessThan(groceries.FinancialStateMultiplier));
            Assert.That(entertainment.Description, Does.Contain("financial stability declined"));
        });
    }

    [Test]
    public async Task CreateAutoMonthlyPlanAsync_WhenGoalNeedsReserve_AppliesIncomeEnvelope()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Categories.AddAsync(new Category
        {
            Id = 10,
            Title = "Entertainment",
            Type = TransactionCategoryType.Expense,
            Priority = CategoryPriority.Discretionary,
            UserId = TestInfrastructure.UserId
        });
        await context.BudgetPlanItems.AddAsync(new BudgetPlanItem
        {
            Id = 10,
            BudgetPlanId = 1,
            CategoryId = 10,
            Amount = 2000m,
            CurrencyId = 1
        });
        await context.FinancialGoals.AddAsync(new FinancialGoal
        {
            Title = "Urgent goal",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = TestInfrastructure.CurrentMonthStart.AddMonths(1).AddDays(15),
            CreatedAt = TestInfrastructure.CurrentMonthStart,
            UserId = TestInfrastructure.UserId
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = CreateManager(context);
        var targetStart = TestInfrastructure.CurrentMonthStart.AddMonths(1);

        var result = await manager.CreateAutoMonthlyPlanAsync(new AutoBudgetPlanRequestDto
        {
            Month = targetStart.Month,
            Year = targetStart.Year
        }, CancellationToken.None);

        var entertainment = result.Items.Single(i => i.CategoryTitle == "Entertainment");

        Assert.Multiple(() =>
        {
            Assert.That(result.GoalReserve, Is.EqualTo(2000m));
            Assert.That(result.ExpenseEnvelope, Is.LessThan(result.NewTotal + result.GoalReserve));
            Assert.That(result.IncomeEnvelopeMultiplier, Is.LessThan(1m));
            Assert.That(entertainment.IncomeEnvelopeMultiplier, Is.LessThan(1m));
            Assert.That(entertainment.Description, Does.Contain("financial goals"));
        });
    }
}
