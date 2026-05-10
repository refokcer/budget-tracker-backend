namespace budget_tracker_backend.Services.AdminData;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.AdminData;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AdminDataManager : IAdminDataManager
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAdminDataTemplateProvider _templateProvider;
    private readonly IAdminDataSampleBuilder _sampleBuilder;

    public AdminDataManager(
        IApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IAdminDataTemplateProvider templateProvider,
        IAdminDataSampleBuilder sampleBuilder)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _templateProvider = templateProvider;
        _sampleBuilder = sampleBuilder;
    }

    public async Task ClearCurrentUserDataAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var transactions = await _context.Transactions.ToListAsync(cancellationToken);
        _context.Transactions.RemoveRange(transactions);

        var goals = await _context.FinancialGoals.ToListAsync(cancellationToken);
        _context.FinancialGoals.RemoveRange(goals);

        var planItems = await _context.BudgetPlanItems.ToListAsync(cancellationToken);
        _context.BudgetPlanItems.RemoveRange(planItems);
        await _context.SaveChangesAsync(cancellationToken);

        var eventPlans = await _context.BudgetPlans
            .Where(p => p.Type == BudgetPlanType.Event)
            .ToListAsync(cancellationToken);
        _context.BudgetPlans.RemoveRange(eventPlans);
        await _context.SaveChangesAsync(cancellationToken);

        var monthlyPlans = await _context.BudgetPlans.ToListAsync(cancellationToken);
        _context.BudgetPlans.RemoveRange(monthlyPlans);

        var accounts = await _context.Accounts.ToListAsync(cancellationToken);
        _context.Accounts.RemoveRange(accounts);

        var categories = await _context.Categories.ToListAsync(cancellationToken);
        _context.Categories.RemoveRange(categories);

        var userClaims = await _context.UserClaims
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.UserClaims.RemoveRange(userClaims);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminDataImportResultDto> ImportAsync(
        AdminDataImportDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.ClearExisting)
            await ClearCurrentUserDataAsync(cancellationToken);

        var userId = GetCurrentUserId();
        var result = new AdminDataImportResultDto();
        var currencyIds = await SeedCurrenciesAsync(dto.Data.Currencies, result, cancellationToken);
        var categoryIds = await SeedCategoriesAsync(dto.Data.Categories, userId, result, cancellationToken);
        var accountIds = await SeedAccountsAsync(dto.Data.Accounts, currencyIds, userId, result, cancellationToken);
        var planIds = await SeedBudgetPlansAsync(
            dto.Data.BudgetPlans,
            categoryIds,
            currencyIds,
            userId,
            result,
            cancellationToken);

        await SeedTransactionsAsync(
            dto.Data.Transactions,
            currencyIds,
            categoryIds,
            accountIds,
            planIds,
            userId,
            result,
            cancellationToken);

        await SeedFinancialGoalsAsync(
            dto.Data.FinancialGoals,
            accountIds,
            userId,
            result,
            cancellationToken);

        return result;
    }

    public IReadOnlyList<AdminDataTemplateDto> GetTemplates()
    {
        return _templateProvider.GetTemplates();
    }

    public async Task<AdminDataImportDto> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken)
    {
        return await _templateProvider.GetTemplateAsync(templateId, cancellationToken);
    }

    public AdminDataImportDto BuildSample()
    {
        return _sampleBuilder.BuildSample();
    }

    private async Task<Dictionary<string, int>> SeedCurrenciesAsync(
        List<AdminCurrencySeedDto> currencies,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var existing = await _context.Currencies.ToListAsync(cancellationToken);

        foreach (var item in currencies)
        {
            RequireKey(item.Key, "currency");
            var currency = existing.FirstOrDefault(c =>
                string.Equals(c.Code, item.Code, StringComparison.OrdinalIgnoreCase)
                || c.Symbol.ToString() == item.Symbol);

            if (currency == null)
            {
                currency = new Currency
                {
                    Code = item.Code.Trim(),
                    Title = item.Title.Trim(),
                    Symbol = string.IsNullOrWhiteSpace(item.Symbol) ? '$' : item.Symbol.Trim()[0],
                    IsBase = item.IsBase
                };
                await _context.Currencies.AddAsync(currency, cancellationToken);
                result.Currencies++;
            }

            map[item.Key] = currency.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var item in currencies.Where(i => map[i.Key] == 0))
        {
            var currency = await _context.Currencies.FirstAsync(
                c => c.Code == item.Code || c.Symbol.ToString() == item.Symbol,
                cancellationToken);
            map[item.Key] = currency.Id;
        }

        if (map.Count == 0)
        {
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.IsBase, cancellationToken)
                ?? await _context.Currencies.FirstOrDefaultAsync(cancellationToken)
                ?? throw new CustomException("No currencies available. Include at least one currency in JSON.", StatusCodes.Status400BadRequest);
            map["default"] = baseCurrency.Id;
        }

        return map;
    }

    private async Task<Dictionary<string, int>> SeedCategoriesAsync(
        List<AdminCategorySeedDto> categories,
        string userId,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entitiesByKey = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in categories)
        {
            RequireKey(item.Key, "category");
            var entity = new Category
            {
                Title = RequireText(item.Title, "category title"),
                Type = ParseEnum<TransactionCategoryType>(item.Type, "category type"),
                Description = item.Description,
                Color = NormalizeColor(item.Color),
                UserId = userId
            };
            await _context.Categories.AddAsync(entity, cancellationToken);
            entitiesByKey[item.Key] = entity;
            result.Categories++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (key, entity) in entitiesByKey)
            map[key] = entity.Id;

        return map;
    }

    private async Task<Dictionary<string, int>> SeedAccountsAsync(
        List<AdminAccountSeedDto> accounts,
        Dictionary<string, int> currencyIds,
        string userId,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entitiesByKey = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in accounts)
        {
            RequireKey(item.Key, "account");
            var entity = new Account
            {
                Title = RequireText(item.Title, "account title"),
                Amount = item.Amount,
                CurrencyId = Resolve(currencyIds, item.CurrencyKey, "currency"),
                Type = ParseEnum<AccountType>(item.Type, "account type"),
                Description = item.Description,
                UserId = userId
            };
            await _context.Accounts.AddAsync(entity, cancellationToken);
            entitiesByKey[item.Key] = entity;
            result.Accounts++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (key, entity) in entitiesByKey)
            map[key] = entity.Id;

        return map;
    }

    private async Task<Dictionary<string, int>> SeedBudgetPlansAsync(
        List<AdminBudgetPlanSeedDto> plans,
        Dictionary<string, int> categoryIds,
        Dictionary<string, int> currencyIds,
        string userId,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var planEntities = new Dictionary<string, BudgetPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in plans)
        {
            RequireKey(item.Key, "budget plan");
            var entity = new BudgetPlan
            {
                Title = RequireText(item.Title, "budget plan title"),
                StartDate = EnsureUtc(item.StartDate),
                EndDate = EnsureUtc(item.EndDate),
                Type = ParseEnum<BudgetPlanType>(item.Type, "budget plan type"),
                Description = item.Description,
                UserId = userId
            };
            await _context.BudgetPlans.AddAsync(entity, cancellationToken);
            planEntities[item.Key] = entity;
            result.BudgetPlans++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (key, entity) in planEntities)
            map[key] = entity.Id;

        foreach (var item in plans.Where(p => !string.IsNullOrWhiteSpace(p.ParentKey)))
        {
            var entity = planEntities[item.Key];
            entity.ParentId = Resolve(map, item.ParentKey!, "parent budget plan");
        }
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var item in plans)
        {
            foreach (var seedItem in item.Items)
            {
                var entity = new BudgetPlanItem
                {
                    BudgetPlanId = map[item.Key],
                    CategoryId = Resolve(categoryIds, seedItem.CategoryKey, "category"),
                    Amount = seedItem.Amount,
                    CurrencyId = Resolve(currencyIds, seedItem.CurrencyKey, "currency"),
                    Description = seedItem.Description
                };
                await _context.BudgetPlanItems.AddAsync(entity, cancellationToken);
                result.BudgetPlanItems++;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        return map;
    }

    private async Task SeedTransactionsAsync(
        List<AdminTransactionSeedDto> transactions,
        Dictionary<string, int> currencyIds,
        Dictionary<string, int> categoryIds,
        Dictionary<string, int> accountIds,
        Dictionary<string, int> planIds,
        string userId,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        foreach (var item in transactions)
        {
            var entity = new Transaction
            {
                Title = RequireText(item.Title, "transaction title"),
                Amount = item.Amount,
                Type = ParseEnum<TransactionCategoryType>(item.Type, "transaction type"),
                Date = EnsureUtc(item.Date),
                CurrencyId = Resolve(currencyIds, item.CurrencyKey, "currency"),
                CategoryId = ResolveOptional(categoryIds, item.CategoryKey),
                BudgetPlanId = ResolveOptional(planIds, item.BudgetPlanKey),
                AccountFrom = ResolveOptional(accountIds, item.AccountFromKey),
                AccountTo = ResolveOptional(accountIds, item.AccountToKey),
                Description = item.Description,
                AuthCode = item.AuthCode,
                UserId = userId
            };
            entity.UnicCode = string.IsNullOrWhiteSpace(item.UnicCode)
                ? GenerateUnicCode(entity)
                : item.UnicCode;
            await _context.Transactions.AddAsync(entity, cancellationToken);
            result.Transactions++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFinancialGoalsAsync(
        List<AdminFinancialGoalSeedDto> goals,
        Dictionary<string, int> accountIds,
        string userId,
        AdminDataImportResultDto result,
        CancellationToken cancellationToken)
    {
        foreach (var item in goals)
        {
            var entity = new FinancialGoal
            {
                Title = RequireText(item.Title, "goal title"),
                TargetAmount = item.TargetAmount,
                InitialAmount = item.InitialAmount,
                TargetDate = EnsureUtc(item.TargetDate),
                CreatedAt = EnsureUtc(item.CreatedAt ?? DateTime.UtcNow),
                LinkedAccountId = ResolveOptional(accountIds, item.LinkedAccountKey),
                Description = item.Description,
                UserId = userId
            };
            await _context.FinancialGoals.AddAsync(entity, cancellationToken);
            result.FinancialGoals++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new CustomException("Current user not found", StatusCodes.Status401Unauthorized);
    }

    private static int Resolve(Dictionary<string, int> map, string? key, string type)
    {
        if (string.IsNullOrWhiteSpace(key) && map.TryGetValue("default", out var defaultId))
            return defaultId;

        if (!string.IsNullOrWhiteSpace(key) && map.TryGetValue(key, out var id))
            return id;

        throw new CustomException($"Unknown {type} key: {key}", StatusCodes.Status400BadRequest);
    }

    private static int? ResolveOptional(Dictionary<string, int> map, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return map.TryGetValue(key, out var id)
            ? id
            : throw new CustomException($"Unknown key: {key}", StatusCodes.Status400BadRequest);
    }

    private static TEnum ParseEnum<TEnum>(string? value, string fieldName)
        where TEnum : struct
    {
        if (Enum.TryParse<TEnum>(value, true, out var parsed))
            return parsed;

        throw new CustomException($"Invalid {fieldName}: {value}", StatusCodes.Status400BadRequest);
    }

    private static string RequireText(string? value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        throw new CustomException($"{fieldName} is required", StatusCodes.Status400BadRequest);
    }

    private static void RequireKey(string? value, string entityName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new CustomException($"{entityName} key is required", StatusCodes.Status400BadRequest);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static string GenerateUnicCode(Transaction transaction)
    {
        using var sha = SHA256.Create();
        var raw = $"{transaction.Title}|{transaction.Amount}|{transaction.Date:O}|{transaction.Type}|{transaction.AuthCode}|{Guid.NewGuid():N}";
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        var trimmed = color.Trim();
        if (trimmed.Length == 6 && trimmed.All(IsHexDigit))
            trimmed = $"#{trimmed}";

        return trimmed.Length == 7
            && trimmed[0] == '#'
            && trimmed.Skip(1).All(IsHexDigit)
                ? trimmed.ToUpperInvariant()
                : throw new CustomException("Category color must be a hex value like #5FB3A7", StatusCodes.Status400BadRequest);
    }

    private static bool IsHexDigit(char value)
    {
        return value is >= '0' and <= '9'
            or >= 'a' and <= 'f'
            or >= 'A' and <= 'F';
    }

}
