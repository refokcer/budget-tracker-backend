namespace budget_tracker_backend.Services.UserSettings;

using System.Security.Claims;
using System.Text.Json;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.UserSettings;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserSettingsManager : IUserSettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        AutoBudgetPlanRulesDto? autoBudgetPlanRules;
        try
        {
            autoBudgetPlanRules = dto.AutoBudgetPlanRules == null
                ? null
                : await NormalizeAutoBudgetPlanRulesAsync(dto.AutoBudgetPlanRules, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(ex.Message);
        }

        await UpsertSettingClaimAsync(userId, UserSettingsClaimTypes.DefaultCurrencyId, dto.DefaultCurrencyId, cancellationToken);
        await UpsertSettingClaimAsync(userId, UserSettingsClaimTypes.DefaultAccountId, dto.DefaultAccountId, cancellationToken);
        if (autoBudgetPlanRules != null)
        {
            await UpsertStringClaimAsync(
                userId,
                UserSettingsClaimTypes.AutoBudgetPlanRules,
                JsonSerializer.Serialize(autoBudgetPlanRules, JsonOptions),
                cancellationToken);
        }

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
                (c.ClaimType == UserSettingsClaimTypes.DefaultCurrencyId
                    || c.ClaimType == UserSettingsClaimTypes.DefaultAccountId
                    || c.ClaimType == UserSettingsClaimTypes.AutoBudgetPlanRules))
            .ToListAsync(cancellationToken);

        var settings = new UserSettingsDto
        {
            DefaultCurrencyId = ReadIntClaim(claims, UserSettingsClaimTypes.DefaultCurrencyId),
            DefaultAccountId = ReadIntClaim(claims, UserSettingsClaimTypes.DefaultAccountId),
            AutoBudgetPlanRules = ReadAutoBudgetPlanRules(claims)
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
            DefaultAccountId = defaultAccountId,
            AutoBudgetPlanRules = await NormalizeAutoBudgetPlanRulesAsync(
                settings.AutoBudgetPlanRules ?? new AutoBudgetPlanRulesDto(),
                cancellationToken)
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

    private async Task UpsertStringClaimAsync(
        string userId,
        string claimType,
        string value,
        CancellationToken cancellationToken)
    {
        var claim = await _context.Set<IdentityUserClaim<string>>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == claimType, cancellationToken);

        if (claim == null)
        {
            await _context.Set<IdentityUserClaim<string>>().AddAsync(
                new IdentityUserClaim<string>
                {
                    UserId = userId,
                    ClaimType = claimType,
                    ClaimValue = value
                },
                cancellationToken);
            return;
        }

        claim.ClaimValue = value;
    }

    private static int? ReadIntClaim(IEnumerable<IdentityUserClaim<string>> claims, string claimType)
    {
        var value = claims.FirstOrDefault(c => c.ClaimType == claimType)?.ClaimValue;
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static AutoBudgetPlanRulesDto ReadAutoBudgetPlanRules(IEnumerable<IdentityUserClaim<string>> claims)
    {
        var value = claims.FirstOrDefault(c => c.ClaimType == UserSettingsClaimTypes.AutoBudgetPlanRules)?.ClaimValue;
        if (string.IsNullOrWhiteSpace(value))
            return new AutoBudgetPlanRulesDto();

        try
        {
            return JsonSerializer.Deserialize<AutoBudgetPlanRulesDto>(value, JsonOptions)
                ?? new AutoBudgetPlanRulesDto();
        }
        catch (JsonException)
        {
            return new AutoBudgetPlanRulesDto();
        }
    }

    private async Task<AutoBudgetPlanRulesDto> NormalizeAutoBudgetPlanRulesAsync(
        AutoBudgetPlanRulesDto rules,
        CancellationToken cancellationToken)
    {
        var normalizedRules = new List<AutoBudgetPlanCategoryRuleDto>();
        foreach (var rule in rules.CategoryRules ?? [])
        {
            if (rule.CategoryId <= 0)
                throw new ArgumentException("Category rule category id is required");

            if (rule.MinimumLimit is < 0m)
                throw new ArgumentException("Minimum category limit cannot be negative");

            if (rule.MaximumLimit is < 0m)
                throw new ArgumentException("Maximum category limit cannot be negative");

            if (rule.MinimumLimit.HasValue
                && rule.MaximumLimit.HasValue
                && rule.MinimumLimit.Value > rule.MaximumLimit.Value)
            {
                throw new ArgumentException("Minimum category limit cannot be greater than maximum category limit");
            }

            var normalizedMonthCoefficients = AutoBudgetPlanRulesDefaults.CreateMonthCoefficients();
            foreach (var coefficient in rule.MonthCoefficients ?? [])
            {
                if (coefficient.Month is < 1 or > 12)
                    throw new ArgumentException("Month coefficient month must be between 1 and 12");

                if (coefficient.Multiplier is < 0m or > 2m)
                    throw new ArgumentException("Month coefficient multiplier must be between 0 and 2");

                normalizedMonthCoefficients[coefficient.Month - 1].Multiplier = Math.Round(coefficient.Multiplier, 4);
            }

            normalizedRules.Add(new AutoBudgetPlanCategoryRuleDto
            {
                CategoryId = rule.CategoryId,
                MinimumLimit = rule.MinimumLimit.HasValue ? Math.Round(rule.MinimumLimit.Value, 2) : null,
                MaximumLimit = rule.MaximumLimit.HasValue ? Math.Round(rule.MaximumLimit.Value, 2) : null,
                CutBehavior = NormalizeCutBehavior(rule.CutBehavior),
                MonthCoefficients = normalizedMonthCoefficients
            });
        }

        normalizedRules = normalizedRules
            .GroupBy(rule => rule.CategoryId)
            .Select(group => group.Last())
            .OrderBy(rule => rule.CategoryId)
            .ToList();
        var allIds = normalizedRules.Select(rule => rule.CategoryId).ToList();

        if (allIds.Count > 0)
        {
            var existingCount = await _context.Categories
                .CountAsync(c => allIds.Contains(c.Id), cancellationToken);
            if (existingCount != allIds.Count)
                throw new ArgumentException("One or more auto plan rule categories were not found");
        }

        return new AutoBudgetPlanRulesDto
        {
            CategoryRules = normalizedRules
        };
    }

    private static string NormalizeCutBehavior(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "protected" => "Protected",
            "aggressive" => "Aggressive",
            _ => "Normal"
        };
    }
}
