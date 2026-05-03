using budget_tracker_backend.Dto.UserSettings;
using budget_tracker_backend.Services.UserSettings;
using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class UserSettingsManagerTests
{
    [Test]
    public async Task UpdateAsync_SavesDefaultCurrencyAndAccount()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        var manager = new UserSettingsManager(context, new HttpContextAccessor
        {
            HttpContext = TestInfrastructure.CreateHttpContext()
        });

        var result = await manager.UpdateAsync(
            new UserSettingsDto { DefaultCurrencyId = 1, DefaultAccountId = 2 },
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.DefaultCurrencyId, Is.EqualTo(1));
            Assert.That(result.Value.DefaultAccountId, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task UpdateAsync_RejectsAccountFromAnotherUser()
    {
        await using var context = TestInfrastructure.CreateContext();
        await TestInfrastructure.SeedReferenceDataAsync(context);
        await context.Accounts.AddAsync(new Account
        {
            Id = 99,
            Title = "Other user's account",
            Amount = 10m,
            CurrencyId = 1,
            UserId = "another-user"
        });
        await context.SaveChangesAsync(CancellationToken.None);
        var manager = new UserSettingsManager(context, new HttpContextAccessor
        {
            HttpContext = TestInfrastructure.CreateHttpContext()
        });

        var result = await manager.UpdateAsync(
            new UserSettingsDto { DefaultCurrencyId = 1, DefaultAccountId = 99 },
            CancellationToken.None);

        Assert.That(result.IsFailed, Is.True);
    }
}
