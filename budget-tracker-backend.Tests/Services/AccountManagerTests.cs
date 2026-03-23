using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class AccountManagerTests
{
    [Test]
    public async Task CreateAsync_PersistsMappedAccount()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        var created = await manager.CreateAsync(new CreateAccountDto
        {
            Title = "Brokerage",
            Amount = 1250m,
            CurrencyId = 1,
            Description = "Investments"
        }, CancellationToken.None);

        var stored = await context.Accounts.FindAsync(created.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(created.UserId, Is.EqualTo(TestInfrastructure.UserId));
            Assert.That(stored!.Title, Is.EqualTo("Brokerage"));
            Assert.That(stored.Amount, Is.EqualTo(1250m));
        });
    }

    [Test]
    public async Task UpdateAsync_WhenAccountExists_UpdatesMutableFields()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        var updated = await manager.UpdateAsync(new AccountDto
        {
            Id = 1,
            Title = "Cash Wallet",
            Amount = 888m,
            CurrencyId = 2,
            Description = "Updated"
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Title, Is.EqualTo("Cash Wallet"));
            Assert.That(updated.Amount, Is.EqualTo(888m));
            Assert.That(updated.CurrencyId, Is.EqualTo(2));
            Assert.That(updated.Description, Is.EqualTo("Updated"));
        });
    }

    [Test]
    public void UpdateAsync_WhenAccountMissing_ThrowsNotFoundCustomException()
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        Assert.That(async () => await manager.UpdateAsync(new AccountDto { Id = 999, Title = "Missing" }, CancellationToken.None),
            Throws.TypeOf<CustomException>().With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task HandleTransactionAsync_ForIncome_ValidatesPositiveAmountAndCreditsDestination()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        var result = await manager.HandleTransactionAsync(TransactionCategoryType.Income, 250m, null, 2, false, CancellationToken.None);
        var target = await context.Accounts.FindAsync(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(target!.Amount, Is.EqualTo(750m));
        });
    }

    [Test]
    public async Task HandleTransactionAsync_WhenExpenseExceedsBalance_ReturnsFailure()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        var result = await manager.HandleTransactionAsync(TransactionCategoryType.Expense, 5000m, 1, null, false, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailed, Is.True);
            Assert.That(result.Errors.First().Message, Is.EqualTo("Not enough money"));
        });
    }

    [Test]
    public async Task DeleteAsync_WhenAccountExists_RemovesEntity()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new AccountManager(context, TestInfrastructure.CreateMapper());

        var deleted = await manager.DeleteAsync(3, CancellationToken.None);
        var stored = await context.Accounts.FindAsync(3);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(stored, Is.Null);
        });
    }
}
