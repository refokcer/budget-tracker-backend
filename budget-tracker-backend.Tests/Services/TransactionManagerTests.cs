using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class TransactionManagerTests
{
    private static TransactionManager CreateManager(ApplicationDbContext context)
    {
        var mapper = TestInfrastructure.CreateMapper();
        var accountManager = new AccountManager(context, mapper);
        return new TransactionManager(context, mapper, accountManager);
    }

    [Test]
    public void CreateAsync_WhenTypeMissing_ThrowsBadRequest()
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = CreateManager(context);

        Assert.That(async () => await manager.CreateAsync(new CreateTransactionDto
        {
            Title = "Broken tx",
            Amount = 15m,
            CurrencyId = 1,
            Date = DateTime.UtcNow,
            Type = TransactionCategoryType.None
        }, CancellationToken.None), Throws.TypeOf<CustomException>()
            .With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task CreateAsync_ForExpense_PersistsTransactionAndAdjustsAccountBalance()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var created = await manager.CreateAsync(new CreateTransactionDto
        {
            Title = "Fuel",
            Amount = 75m,
            CurrencyId = 1,
            CategoryId = 2,
            AccountFrom = 1,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(17),
            Type = TransactionCategoryType.Expense,
            AuthCode = "AUTH-75"
        }, CancellationToken.None);

        var account = await context.Accounts.FindAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.GreaterThan(0));
            Assert.That(created.UnicCode, Has.Length.EqualTo(64));
            Assert.That(account!.Amount, Is.EqualTo(925m));
        });
    }

    [Test]
    public async Task UpdateAsync_ReversesPreviousEffectAndAppliesNewBalances()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var updated = await manager.UpdateAsync(new UpdateTransactionDto
        {
            Id = 2,
            Amount = 50m,
            AccountFrom = 2,
            Date = TestInfrastructure.CurrentMonthStart.AddDays(8),
            AuthCode = "NEW-AUTH"
        }, CancellationToken.None);

        var oldAccount = await context.Accounts.FindAsync(1);
        var newAccount = await context.Accounts.FindAsync(2);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Amount, Is.EqualTo(50m));
            Assert.That(updated.AccountFrom, Is.EqualTo(2));
            Assert.That(oldAccount!.Amount, Is.EqualTo(1120m));
            Assert.That(newAccount!.Amount, Is.EqualTo(450m));
            Assert.That(updated.UnicCode, Has.Length.EqualTo(64));
        });
    }

    [Test]
    public async Task DeleteAsync_RemovesTransactionAndRestoresBalances()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var deleted = await manager.DeleteAsync(3, CancellationToken.None);
        var from = await context.Accounts.FindAsync(1);
        var to = await context.Accounts.FindAsync(2);
        var stored = await context.Transactions.FindAsync(3);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(from!.Amount, Is.EqualTo(1300m));
            Assert.That(to!.Amount, Is.EqualTo(200m));
            Assert.That(stored, Is.Null);
        });
    }

    [Test]
    public async Task GetFilteredDetailedAsync_AppliesAllProvidedFilters()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var result = (await manager.GetFilteredDetailedAsync(
            TransactionCategoryType.Expense,
            2,
            TestInfrastructure.CurrentMonthStart,
            TestInfrastructure.CurrentMonthStart.AddMonths(1).AddTicks(-1),
            1,
            1,
            null,
            CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Weekly groceries"));
            Assert.That(result[0].Category!.Title, Is.EqualTo("Groceries"));
        });
    }

    [Test]
    public async Task PrepareAsync_UsesReferenceDataAndSkipsDuplicateTransactions()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = CreateManager(context);

        var prepared = (await manager.PrepareAsync(
        [
            new ImportTransactionDto
            {
                Title = "Weekly groceries",
                Amount = -120m,
                Currency = "USD",
                AccountFrom = 1,
                Date = TestInfrastructure.CurrentMonthStart.AddDays(6),
                Type = TransactionCategoryType.Expense,
                AuthCode = null
            },
            new ImportTransactionDto
            {
                Title = "Weekly groceries",
                Amount = -80m,
                Currency = "USD",
                AccountFrom = 1,
                Date = TestInfrastructure.CurrentMonthStart.AddDays(19),
                Type = TransactionCategoryType.Expense,
                AuthCode = "A2"
            }
        ], CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(prepared, Has.Count.EqualTo(1));
            Assert.That(prepared[0].Amount, Is.EqualTo(80m));
            Assert.That(prepared[0].CurrencyId, Is.EqualTo(1));
            Assert.That(prepared[0].BudgetPlanId, Is.EqualTo(1));
            Assert.That(prepared[0].CategoryId, Is.EqualTo(2));
        });
    }
}
