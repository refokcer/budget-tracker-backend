using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class FinancialGoalManagerTests
{
    private static FinancialGoalManager CreateManager(ApplicationDbContext context)
    {
        return new FinancialGoalManager(context, TestInfrastructure.CreateMapper());
    }

    [Test]
    public async Task CreateAsync_PersistsGoalWithSavingsAccount()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var created = await manager.CreateAsync(new CreateFinancialGoalDto
        {
            Title = "Vacation",
            TargetAmount = 2500m,
            InitialAmount = 100m,
            TargetDate = DateTime.UtcNow.AddMonths(6),
            LinkedAccountId = 2,
            Description = "Summer trip"
        }, CancellationToken.None);

        var stored = await context.FinancialGoals.FindAsync(created.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Title, Is.EqualTo("Vacation"));
            Assert.That(stored.LinkedAccountId, Is.EqualTo(2));
            Assert.That(stored.UserId, Is.EqualTo(TestInfrastructure.UserId));
        });
    }

    [Test]
    public void CreateAsync_WhenLinkedAccountIsNotSavings_ThrowsCustomException()
    {
        using var context = TestInfrastructure.CreateContext();
        TestInfrastructure.SeedReferenceDataAsync(context).GetAwaiter().GetResult();
        var manager = CreateManager(context);

        Assert.That(async () => await manager.CreateAsync(new CreateFinancialGoalDto
        {
            Title = "Laptop",
            TargetAmount = 1800m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(3),
            LinkedAccountId = 1
        }, CancellationToken.None),
            Throws.TypeOf<CustomException>().With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetForecastAsync_WhenGoalNeedsAdditionalSavings_ReturnsWarningsAndAdaptiveSuggestions()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await SeedForecastScenarioAsync(context);
        var manager = CreateManager(context);

        var goal = new FinancialGoal
        {
            Title = "New laptop",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            Description = "Work upgrade",
            UserId = TestInfrastructure.UserId
        };

        await context.FinancialGoals.AddAsync(goal);
        await context.SaveChangesAsync(CancellationToken.None);

        var forecast = await manager.GetForecastAsync(goal.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(forecast.IsAchievable, Is.False);
            Assert.That(forecast.ContributionGap, Is.GreaterThan(0m));
            Assert.That(forecast.SuggestedBudgetAdjustments, Is.Not.Empty);
            Assert.That(forecast.SuggestedBudgetAdjustments.Select(x => x.CategoryTitle),
                Does.Contain("Entertainment").And.Contain("Dining Out"));
            Assert.That(forecast.SuggestedBudgetAdjustments.All(x => x.SuggestedReduction <= Math.Round(x.CurrentBudgetLimit * 0.30m, 2)), Is.True);
            Assert.That(forecast.SuggestedBudgetAdjustments.All(x => x.RecommendedBudgetLimit >= 0m), Is.True);
            Assert.That(forecast.Warnings, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ApplyBudgetAdjustmentsAsync_WhenForecastHasSuggestions_UpdatesCurrentMonthlyPlan()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await SeedForecastScenarioAsync(context);
        var manager = CreateManager(context);

        var goal = new FinancialGoal
        {
            Title = "Emergency laptop",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            UserId = TestInfrastructure.UserId
        };

        await context.FinancialGoals.AddAsync(goal);
        await context.SaveChangesAsync(CancellationToken.None);

        var entertainmentItemBefore = await context.BudgetPlanItems
            .FirstAsync(i => i.BudgetPlanId == 1 && i.CategoryId == 5);
        var entertainmentAmountBefore = entertainmentItemBefore.Amount;

        var result = await manager.ApplyBudgetAdjustmentsAsync(goal.Id, null, CancellationToken.None);
        var currentPlan = await context.BudgetPlans.Include(p => p.Items).FirstAsync(p => p.Id == 1);
        var planItems = currentPlan.Items ?? throw new AssertionException("Expected current plan items to be loaded.");
        var entertainmentItemAfter = planItems.First(i => i.CategoryId == 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.BudgetPlanId, Is.EqualTo(1));
            Assert.That(result.AppliedAdjustmentsCount, Is.GreaterThan(0));
            Assert.That(entertainmentItemAfter.Amount, Is.LessThan(entertainmentAmountBefore));
            Assert.That(planItems.Count(i => i.CategoryId == 5), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ApplyBudgetAdjustmentsAsync_WhenCategoryAlreadyAdjusted_DoesNotSuggestItAgain()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await SeedForecastScenarioAsync(context);
        var manager = CreateManager(context);

        var goal = new FinancialGoal
        {
            Title = "No duplicate adjustment",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            UserId = TestInfrastructure.UserId
        };

        await context.FinancialGoals.AddAsync(goal);
        await context.SaveChangesAsync(CancellationToken.None);

        var firstResult = await manager.ApplyBudgetAdjustmentsAsync(goal.Id, null, CancellationToken.None);
        var secondForecast = await manager.GetForecastAsync(goal.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.AppliedAdjustmentsCount, Is.GreaterThan(0));
            Assert.That(
                secondForecast.SuggestedBudgetAdjustments.Select(s => s.CategoryId)
                    .Intersect(firstResult.AppliedAdjustments.Select(a => a.CategoryId)),
                Is.Empty);
        });
    }

    [Test]
    public async Task ApplyBudgetAdjustmentsAsync_WhenRequestEditsAndRemovesSuggestions_AppliesEditedRowsOnly()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await SeedForecastScenarioAsync(context);
        var manager = CreateManager(context);

        var goal = new FinancialGoal
        {
            Title = "Editable adjustment",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            UserId = TestInfrastructure.UserId
        };

        await context.FinancialGoals.AddAsync(goal);
        await context.SaveChangesAsync(CancellationToken.None);

        var forecast = await manager.GetForecastAsync(goal.Id, CancellationToken.None);
        var firstSuggestion = forecast.SuggestedBudgetAdjustments.First();
        var removedSuggestion = forecast.SuggestedBudgetAdjustments.Skip(1).First();
        var editedReduction = Math.Round(firstSuggestion.SuggestedReduction / 2m, 2);
        var editedLimit = Math.Round(firstSuggestion.CurrentBudgetLimit - editedReduction, 2);
        var removedItemBefore = await context.BudgetPlanItems
            .AsNoTracking()
            .FirstAsync(i => i.BudgetPlanId == 1 && i.CategoryId == removedSuggestion.CategoryId);

        var result = await manager.ApplyBudgetAdjustmentsAsync(
            goal.Id,
            new ApplyBudgetAdjustmentsRequestDto
            {
                Adjustments = new List<BudgetAdjustmentSuggestionDto>
                {
                    new()
                    {
                        CategoryId = firstSuggestion.CategoryId,
                        RecommendedBudgetLimit = editedLimit
                    }
                }
            },
            CancellationToken.None);

        var editedItemAfter = await context.BudgetPlanItems
            .AsNoTracking()
            .FirstAsync(i => i.BudgetPlanId == 1 && i.CategoryId == firstSuggestion.CategoryId);
        var removedItemAfter = await context.BudgetPlanItems
            .AsNoTracking()
            .FirstAsync(i => i.BudgetPlanId == 1 && i.CategoryId == removedSuggestion.CategoryId);

        Assert.Multiple(() =>
        {
            Assert.That(result.AppliedAdjustmentsCount, Is.EqualTo(1));
            Assert.That(result.AppliedAdjustments.Single().SuggestedReduction, Is.EqualTo(editedReduction));
            Assert.That(editedItemAfter.Amount, Is.EqualTo(editedLimit));
            Assert.That(removedItemAfter.Amount, Is.EqualTo(removedItemBefore.Amount));
        });
    }

    [Test]
    public async Task ApplyBudgetAdjustmentsAsync_WhenRequestExceedsAllowedReduction_ThrowsCustomException()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await SeedForecastScenarioAsync(context);
        var manager = CreateManager(context);

        var goal = new FinancialGoal
        {
            Title = "Unsafe adjustment",
            TargetAmount = 2000m,
            InitialAmount = 0m,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            LinkedAccountId = 2,
            UserId = TestInfrastructure.UserId
        };

        await context.FinancialGoals.AddAsync(goal);
        await context.SaveChangesAsync(CancellationToken.None);

        var forecast = await manager.GetForecastAsync(goal.Id, CancellationToken.None);
        var suggestion = forecast.SuggestedBudgetAdjustments.First();
        var tooAggressiveLimit = Math.Round(suggestion.RecommendedBudgetLimit - 1m, 2);

        Assert.That(async () => await manager.ApplyBudgetAdjustmentsAsync(
            goal.Id,
            new ApplyBudgetAdjustmentsRequestDto
            {
                Adjustments = new List<BudgetAdjustmentSuggestionDto>
                {
                    new()
                    {
                        CategoryId = suggestion.CategoryId,
                        RecommendedBudgetLimit = tooAggressiveLimit
                    }
                }
            },
            CancellationToken.None),
            Throws.TypeOf<CustomException>().With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
    }

    private static async Task SeedForecastScenarioAsync(ApplicationDbContext context)
    {
        if (!await context.Categories.AnyAsync(c => c.Id == 5))
        {
            await context.Categories.AddRangeAsync(
                new Category { Id = 5, Title = "Entertainment", Type = TransactionCategoryType.Expense, UserId = TestInfrastructure.UserId },
                new Category { Id = 6, Title = "Dining Out", Type = TransactionCategoryType.Expense, UserId = TestInfrastructure.UserId },
                new Category { Id = 7, Title = "Shopping", Type = TransactionCategoryType.Expense, UserId = TestInfrastructure.UserId });
        }

        var incomeMonths = new[]
        {
            TestInfrastructure.CurrentMonthStart.AddMonths(-2),
            TestInfrastructure.CurrentMonthStart.AddMonths(-1),
            TestInfrastructure.CurrentMonthStart
        };

        var nextId = await context.Transactions.MaxAsync(t => (int?)t.Id) ?? 0;
        foreach (var month in incomeMonths)
        {
            nextId++;
            await context.Transactions.AddAsync(new Transaction
            {
                Id = nextId,
                Title = $"Salary {month:yyyy-MM}",
                Amount = 3000m,
                CategoryId = 1,
                CurrencyId = 1,
                Date = month.AddDays(2),
                Type = TransactionCategoryType.Income,
                AccountTo = 1,
                UserId = TestInfrastructure.UserId,
                UnicCode = $"salary-{month:yyyyMM}"
            });

            nextId++;
            await context.Transactions.AddAsync(new Transaction
            {
                Id = nextId,
                Title = $"Groceries {month:yyyy-MM}",
                Amount = 2200m,
                CategoryId = 2,
                CurrencyId = 1,
                BudgetPlanId = 1,
                Date = month.AddDays(4),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = TestInfrastructure.UserId,
                UnicCode = $"groceries-{month:yyyyMM}"
            });

            nextId++;
            await context.Transactions.AddAsync(new Transaction
            {
                Id = nextId,
                Title = $"Entertainment {month:yyyy-MM}",
                Amount = 500m,
                CategoryId = 5,
                CurrencyId = 1,
                Date = month.AddDays(6),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = TestInfrastructure.UserId,
                UnicCode = $"entertainment-{month:yyyyMM}"
            });

            nextId++;
            await context.Transactions.AddAsync(new Transaction
            {
                Id = nextId,
                Title = $"Dining {month:yyyy-MM}",
                Amount = 350m,
                CategoryId = 6,
                CurrencyId = 1,
                Date = month.AddDays(8),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = TestInfrastructure.UserId,
                UnicCode = $"dining-{month:yyyyMM}"
            });

            nextId++;
            await context.Transactions.AddAsync(new Transaction
            {
                Id = nextId,
                Title = $"Shopping {month:yyyy-MM}",
                Amount = 300m,
                CategoryId = 7,
                CurrencyId = 1,
                Date = month.AddDays(10),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = TestInfrastructure.UserId,
                UnicCode = $"shopping-{month:yyyyMM}"
            });
        }

        var savings = await context.Accounts.FindAsync(2);
        Assert.That(savings, Is.Not.Null);
        savings!.Amount = 500m;

        if (!await context.BudgetPlanItems.AnyAsync(i => i.BudgetPlanId == 1 && i.CategoryId == 5))
        {
            await context.BudgetPlanItems.AddRangeAsync(
                new BudgetPlanItem
                {
                    Id = 5,
                    BudgetPlanId = 1,
                    CategoryId = 5,
                    Amount = 800m,
                    CurrencyId = 1,
                    Description = "Entertainment limit"
                },
                new BudgetPlanItem
                {
                    Id = 6,
                    BudgetPlanId = 1,
                    CategoryId = 6,
                    Amount = 600m,
                    CurrencyId = 1,
                    Description = "Dining limit"
                },
                new BudgetPlanItem
                {
                    Id = 7,
                    BudgetPlanId = 1,
                    CategoryId = 7,
                    Amount = 500m,
                    CurrencyId = 1,
                    Description = "Shopping limit"
                });
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }
}
