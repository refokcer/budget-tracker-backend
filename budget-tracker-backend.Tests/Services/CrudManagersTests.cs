using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class CrudManagersTests
{
    [Test]
    public async Task CategoryManager_CreateUpdateDelete_CoversHappyPath()
    {
        await using var context = TestInfrastructure.CreateContext();
        var mapper = TestInfrastructure.CreateMapper();
        var manager = new CategoryManager(context, mapper);

        var created = await manager.CreateAsync(new CreateCategoryDto
        {
            Title = "Transport",
            Type = (int)TransactionCategoryType.Expense,
            Description = "Bus and taxi"
        }, CancellationToken.None);

        var updated = await manager.UpdateAsync(new CategoryDto
        {
            Id = created.Id,
            Title = "Transport Updated",
            Type = (int)TransactionCategoryType.Expense,
            Description = "Taxi only"
        }, CancellationToken.None);

        var deleted = await manager.DeleteAsync(created.Id, CancellationToken.None);
        var stored = await context.Categories.FindAsync(created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(created.UserId, Is.EqualTo(TestInfrastructure.UserId));
            Assert.That(updated.Title, Is.EqualTo("Transport Updated"));
            Assert.That(deleted, Is.True);
            Assert.That(stored, Is.Null);
        });
    }

    [Test]
    public void CategoryManager_DeleteAsync_WhenCategoryMissing_ThrowsNotFound()
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = new CategoryManager(context, TestInfrastructure.CreateMapper());

        Assert.That(async () => await manager.DeleteAsync(404, CancellationToken.None),
            Throws.TypeOf<CustomException>().With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task CurrencyManager_CreateAndGetById_PersistsCurrency()
    {
        await using var context = TestInfrastructure.CreateContext();
        var manager = new CurrencyManager(context, TestInfrastructure.CreateMapper());

        var created = await manager.CreateAsync(new CreateCurrencyDto
        {
            Title = "Polish Zloty",
            Code = "PLN",
            Symbol = "z",
            IsBase = false
        }, CancellationToken.None);

        var fetched = await manager.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.Code, Is.EqualTo("PLN"));
            Assert.That(fetched.Symbol, Is.EqualTo('z'));
        });
    }

    [Test]
    public async Task BudgetPlanManager_CreateAsync_WhenEventParentIsMonthly_SetsParentId()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new BudgetPlanManager(context, TestInfrastructure.CreateMapper());

        var created = await manager.CreateAsync(new CreateBudgetPlanDto
        {
            Title = "Conference",
            StartDate = TestInfrastructure.CurrentMonthStart.AddDays(19),
            EndDate = TestInfrastructure.CurrentMonthStart.AddDays(20),
            Type = BudgetPlanType.Event,
            ParentId = 1,
            Description = "Work trip"
        }, CancellationToken.None);

        Assert.That(created.ParentId, Is.EqualTo(1));
    }

    [Test]
    public void BudgetPlanManager_CreateAsync_WhenMonthlyHasParent_ThrowsBadRequest()
    {
        using var context = TestInfrastructure.CreateContext();
        var manager = new BudgetPlanManager(context, TestInfrastructure.CreateMapper());

        Assert.That(async () => await manager.CreateAsync(new CreateBudgetPlanDto
        {
            Title = "Invalid",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            Type = BudgetPlanType.Monthly,
            ParentId = 1
        }, CancellationToken.None), Throws.TypeOf<CustomException>()
            .With.Property(nameof(CustomException.StatusCode)).EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task BudgetPlanItemManager_GetByPlanIdAsync_ReturnsRelatedCategoryAndCurrency()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new BudgetPlanItemManager(context, TestInfrastructure.CreateMapper());

        var items = (await manager.GetByPlanIdAsync(1, CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].Category!.Title, Is.EqualTo("Groceries"));
            Assert.That(items[0].Currency.Symbol, Is.EqualTo('$'));
        });
    }

    [Test]
    public async Task BudgetPlanItemManager_UpdateAsync_UpdatesItemValues()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new BudgetPlanItemManager(context, TestInfrastructure.CreateMapper());

        var updated = await manager.UpdateAsync(new BudgetPlanItemDto
        {
            Id = 1,
            BudgetPlanId = 1,
            CategoryId = 2,
            Amount = 450m,
            CurrencyId = 2,
            Description = "Revised budget"
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Amount, Is.EqualTo(450m));
            Assert.That(updated.CurrencyId, Is.EqualTo(2));
            Assert.That(updated.Description, Is.EqualTo("Revised budget"));
        });
    }

    [Test]
    public async Task BudgetPlanItemManager_CreateAsync_WithZeroCategoryId_CreatesOtherBudgetItem()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new BudgetPlanItemManager(context, TestInfrastructure.CreateMapper());

        var created = await manager.CreateAsync(new CreateBudgetPlanItemDto
        {
            BudgetPlanId = 1,
            CategoryId = 0,
            Amount = 100m,
            CurrencyId = 1,
            Description = "Flexible budget"
        }, CancellationToken.None);

        var category = await context.Categories.FirstAsync(c => c.Id == created.CategoryId);

        Assert.Multiple(() =>
        {
            Assert.That(category.Title, Is.EqualTo("Other"));
            Assert.That(category.Type, Is.EqualTo(TransactionCategoryType.Expense));
            Assert.That(created.Amount, Is.EqualTo(100m));
        });
    }
}
