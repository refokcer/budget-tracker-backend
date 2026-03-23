using budget_tracker_backend.Extensions;

namespace budget_tracker_backend.Tests.Extensions;

[TestFixture]
public class QueryableExtensionsTests
{
    [Test]
    public void WhereIf_WhenConditionTrue_AppliesPredicate()
    {
        var source = new[] { 1, 2, 3, 4 }.AsQueryable();

        var result = source.WhereIf(true, value => value % 2 == 0).ToArray();

        Assert.That(result, Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public void WhereIf_WhenConditionFalse_ReturnsOriginalSequence()
    {
        var source = new[] { 1, 2, 3, 4 }.AsQueryable();

        var result = source.WhereIf(false, value => value % 2 == 0).ToArray();

        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }
}
