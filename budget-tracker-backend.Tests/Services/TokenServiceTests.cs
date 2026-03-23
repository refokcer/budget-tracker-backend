using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace budget_tracker_backend.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private static IConfiguration CreateConfiguration(string key = "0123456789abcdef0123456789abcdef") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "budget-tracker-tests",
                ["Jwt:Audience"] = "budget-tracker-clients"
            })
            .Build();

    [Test]
    public void CreateAccessToken_EmbedsIdentityAndRoles()
    {
        var service = new TokenService(CreateConfiguration());
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "demo-user",
            Email = "demo@example.com"
        };

        var token = service.CreateAccessToken(user, ["Admin", "User"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Multiple(() =>
        {
            Assert.That(jwt.Issuer, Is.EqualTo("budget-tracker-tests"));
            Assert.That(jwt.Audiences, Contains.Item("budget-tracker-clients"));
            Assert.That(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value, Is.EqualTo("user-1"));
            Assert.That(jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value), Is.EquivalentTo(new[] { "Admin", "User" }));
        });
    }

    [Test]
    public void CreateAccessToken_WhenKeyTooShort_ThrowsArgumentOutOfRangeException()
    {
        var service = new TokenService(CreateConfiguration("short-key"));

        Assert.That(() => service.CreateAccessToken(new ApplicationUser { Id = "user-1" }, []),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void CreateRefreshToken_GeneratesActiveTokenWithSevenDayLifetime()
    {
        var service = new TokenService(CreateConfiguration());

        var token = service.CreateRefreshToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.Token, Is.Not.Empty);
            Assert.That(token.IsActive, Is.True);
            Assert.That(token.Expires, Is.GreaterThan(token.Created.AddDays(6.99)));
            Assert.That(token.Expires, Is.LessThanOrEqualTo(token.Created.AddDays(7.01)));
        });
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WhenTokenSignatureValidationFails_ThrowsSecurityTokenException()
    {
        var config = CreateConfiguration();
        var service = new TokenService(config);
        var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"));
        var token = new JwtSecurityToken(
            issuer: "budget-tracker-tests",
            audience: "budget-tracker-clients",
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512));

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);

        Assert.That(() => service.GetPrincipalFromExpiredToken(serialized),
            Throws.InstanceOf<SecurityTokenException>());
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WhenTokenIsValid_ReturnsPrincipal()
    {
        var service = new TokenService(CreateConfiguration());
        var token = service.CreateAccessToken(new ApplicationUser { Id = "user-42", UserName = "user42" }, ["User"]);

        var principal = service.GetPrincipalFromExpiredToken(token);

        Assert.That(principal?.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("user-42"));
    }
}
