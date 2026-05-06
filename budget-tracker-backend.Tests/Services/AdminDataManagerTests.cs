using budget_tracker_backend.Dto.AdminData;
using budget_tracker_backend.Services.AdminData;
using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class AdminDataManagerTests
{
    private static AdminDataManager CreateManager(ApplicationDbContext context)
    {
        return new AdminDataManager(
            context,
            new HttpContextAccessor { HttpContext = TestInfrastructure.CreateHttpContext() });
    }

    [Test]
    public async Task ImportAsync_WithSampleJson_CreatesSixMonthsOfSimulationData()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);
        var sample = manager.BuildSample();

        var result = await manager.ImportAsync(sample, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accounts, Is.EqualTo(3));
            Assert.That(result.Categories, Is.EqualTo(8));
            Assert.That(result.BudgetPlans, Is.EqualTo(6));
            Assert.That(result.Transactions, Is.EqualTo(48));
            Assert.That(result.FinancialGoals, Is.EqualTo(1));
            Assert.That(context.BudgetPlans.Count(), Is.EqualTo(6));
            Assert.That(context.Transactions.Count(), Is.EqualTo(48));
        });
    }

    [Test]
    public async Task ClearCurrentUserDataAsync_RemovesUserOwnedDataAndKeepsCurrencies()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        await manager.ClearCurrentUserDataAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(context.Accounts.Count(), Is.EqualTo(0));
            Assert.That(context.Categories.Count(), Is.EqualTo(0));
            Assert.That(context.BudgetPlans.Count(), Is.EqualTo(0));
            Assert.That(context.BudgetPlanItems.Count(), Is.EqualTo(0));
            Assert.That(context.Transactions.Count(), Is.EqualTo(0));
            Assert.That(context.FinancialGoals.Count(), Is.EqualTo(0));
            Assert.That(context.Currencies.Count(), Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task ImportAsync_ResolvesReferencesByJsonKeys()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.ImportAsync(new AdminDataImportDto
        {
            ClearExisting = true,
            Data = new AdminDataSeedDto
            {
                Currencies =
                [
                    new() { Key = "usd", Code = "USD", Title = "US Dollar", Symbol = "$", IsBase = true }
                ],
                Categories =
                [
                    new() { Key = "income", Title = "Income", Type = "Income" },
                    new() { Key = "food", Title = "Food", Type = "Expense" }
                ],
                Accounts =
                [
                    new() { Key = "main", Title = "Main", Amount = 100m, CurrencyKey = "usd", Type = "Checking" }
                ],
                BudgetPlans =
                [
                    new()
                    {
                        Key = "plan",
                        Title = "Plan",
                        StartDate = TestInfrastructure.CurrentMonthStart,
                        EndDate = TestInfrastructure.CurrentMonthStart.AddMonths(1).AddDays(-1),
                        Type = "Monthly",
                        Items =
                        [
                            new() { CategoryKey = "food", Amount = 200m, CurrencyKey = "usd" }
                        ]
                    }
                ],
                Transactions =
                [
                    new()
                    {
                        Title = "Food tx",
                        Amount = 50m,
                        Type = "Expense",
                        Date = TestInfrastructure.CurrentMonthStart.AddDays(3),
                        CurrencyKey = "usd",
                        CategoryKey = "food",
                        BudgetPlanKey = "plan",
                        AccountFromKey = "main"
                    }
                ]
            }
        }, CancellationToken.None);

        var transaction = await context.Transactions.FirstAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Transactions, Is.EqualTo(1));
            Assert.That(transaction.CategoryId, Is.Not.Null);
            Assert.That(transaction.BudgetPlanId, Is.Not.Null);
            Assert.That(transaction.AccountFrom, Is.Not.Null);
        });
    }
}
