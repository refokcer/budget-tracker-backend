using budget_tracker_backend.Tests.Common;

namespace budget_tracker_backend.Tests.Mapping;

[TestFixture]
public class MappingConfigurationTests
{
    [Test]
    public void AutoMapperConfiguration_IsValidForAllProfiles()
    {
        Assert.That(() => TestInfrastructure.CreateMapper(), Throws.Nothing);
    }

    [Test]
    public void TransactionProfile_IgnoresUnicCodeWhenMappingCreateDto()
    {
        var mapper = TestInfrastructure.CreateMapper();

        var entity = mapper.Map<Transaction>(new CreateTransactionDto
        {
            Title = "Coffee",
            Amount = 10m,
            CurrencyId = 1,
            Date = TestInfrastructure.CurrentMonthStart,
            Type = TransactionCategoryType.Expense,
            AuthCode = "AUTH"
        });

        Assert.Multiple(() =>
        {
            Assert.That(entity.Title, Is.EqualTo("Coffee"));
            Assert.That(entity.UnicCode, Is.Null.Or.Empty);
        });
    }
}
