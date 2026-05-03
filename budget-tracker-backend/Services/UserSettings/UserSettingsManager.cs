namespace budget_tracker_backend.Services.UserSettings;

using System.Security.Claims;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.UserSettings;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserSettingsManager : IUserSettingsManager
{
    private const string DefaultCurrencyClaimType = "settings:default-currency-id";
    private const string DefaultAccountClaimType = "settings:default-account-id";

    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSettingsManager(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<UserSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Fail("User not found");
        }

        return Result.Ok(await BuildDtoAsync(userId, cancellationToken));
    }

    public async Task<Result<UserSettingsDto>> UpdateAsync(UserSettingsDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Fail("User not found");
        }

        if (dto.DefaultCurrencyId.HasValue)
        {
            var currencyExists = await _context.Currencies
                .AnyAsync(c => c.Id == dto.DefaultCurrencyId.Value, cancellationToken);

            if (!currencyExists)
            {
                return Result.Fail("Default currency not found");
            }
        }

        if (dto.DefaultAccountId.HasValue)
        {
            var accountExists = await _context.Accounts
                .AnyAsync(a => a.Id == dto.DefaultAccountId.Value, cancellationToken);

            if (!accountExists)
            {
                return Result.Fail("Default account not found");
            }
        }

        await UpsertSettingClaimAsync(userId, DefaultCurrencyClaimType, dto.DefaultCurrencyId, cancellationToken);
        await UpsertSettingClaimAsync(userId, DefaultAccountClaimType, dto.DefaultAccountId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok(await BuildDtoAsync(userId, cancellationToken));
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<UserSettingsDto> BuildDtoAsync(string userId, CancellationToken cancellationToken)
    {
        var claims = await _context.Set<IdentityUserClaim<string>>()
            .AsNoTracking()
            .Where(c => c.UserId == userId &&
                (c.ClaimType == DefaultCurrencyClaimType || c.ClaimType == DefaultAccountClaimType))
            .ToListAsync(cancellationToken);

        var settings = new UserSettingsDto
        {
            DefaultCurrencyId = ReadIntClaim(claims, DefaultCurrencyClaimType),
            DefaultAccountId = ReadIntClaim(claims, DefaultAccountClaimType)
        };

        var defaultCurrencyId = settings.DefaultCurrencyId.HasValue &&
            await _context.Currencies.AnyAsync(c => c.Id == settings.DefaultCurrencyId.Value, cancellationToken)
                ? settings.DefaultCurrencyId
                : null;

        var defaultAccountId = settings.DefaultAccountId.HasValue &&
            await _context.Accounts.AnyAsync(a => a.Id == settings.DefaultAccountId.Value, cancellationToken)
                ? settings.DefaultAccountId
                : null;

        return new UserSettingsDto
        {
            DefaultCurrencyId = defaultCurrencyId,
            DefaultAccountId = defaultAccountId
        };
    }

    private async Task UpsertSettingClaimAsync(
        string userId,
        string claimType,
        int? value,
        CancellationToken cancellationToken)
    {
        var claim = await _context.Set<IdentityUserClaim<string>>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == claimType, cancellationToken);

        if (!value.HasValue)
        {
            if (claim != null)
            {
                _context.Remove(claim);
            }

            return;
        }

        if (claim == null)
        {
            await _context.Set<IdentityUserClaim<string>>().AddAsync(
                new IdentityUserClaim<string>
                {
                    UserId = userId,
                    ClaimType = claimType,
                    ClaimValue = value.Value.ToString()
                },
                cancellationToken);
            return;
        }

        claim.ClaimValue = value.Value.ToString();
    }

    private static int? ReadIntClaim(IEnumerable<IdentityUserClaim<string>> claims, string claimType)
    {
        var value = claims.FirstOrDefault(c => c.ClaimType == claimType)?.ClaimValue;
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
