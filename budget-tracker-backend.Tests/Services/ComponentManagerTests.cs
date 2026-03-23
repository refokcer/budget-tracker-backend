using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class ComponentManagerTests
{
    private static ComponentManager CreateManager(ApplicationDbContext context)
    {
        var mapper = TestInfrastructure.CreateMapper();
        return new ComponentManager(
            new AccountManager(context, mapper),
            new CategoryManager(context, mapper),
            new CurrencyManager(context, mapper),
            new BudgetPlanManager(context, mapper),
            mapper);
    }

    [Test]
    public async Task GetExpenseModalAsync_ReturnsCurrenciesCategoriesAccountsAndPlans()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.GetExpenseModalAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Currencies.Select(c => c.Code), Is.EquivalentTo(new[] { "USD", "EUR" }));
            Assert.That(result.Categories.Select(c => c.Title), Is.EquivalentTo(new[] { "Groceries" }));
            Assert.That(result.Accounts, Has.Exactly(3).Items);
            Assert.That(result.Plans.Select(p => p.Title), Is.EquivalentTo(new[] { "March Plan", "Birthday Event" }));
        });
    }

    [Test]
    public async Task GetTransferModalAsync_ReturnsTransferCategoriesAndAccounts()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = await manager.GetTransferModalAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Categories.Select(c => c.Title), Is.EquivalentTo(new[] { "Transfer" }));
            Assert.That(result.Accounts.Select(a => a.Title), Does.Contain("Cash"));
            Assert.That(result.Currencies.Select(c => c.Symbol), Does.Contain("$"));
        });
    }
}
