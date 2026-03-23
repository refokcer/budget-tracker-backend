namespace budget_tracker_backend.Tests.Models;

[TestFixture]
public class RefreshTokenTests
{
    [Test]
    public void IsActive_WhenTokenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            Expires = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.Multiple(() =>
        {
            Assert.That(token.IsExpired, Is.False);
            Assert.That(token.IsActive, Is.True);
        });
    }

    [Test]
    public void IsActive_WhenTokenRevoked_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            Expires = DateTime.UtcNow.AddMinutes(5),
            Revoked = DateTime.UtcNow
        };

        Assert.That(token.IsActive, Is.False);
    }
}
