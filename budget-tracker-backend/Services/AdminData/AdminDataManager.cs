namespace budget_tracker_backend.Services.AdminData;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.AdminData;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AdminDataManager : IAdminDataManager
{
    private static readonly IReadOnlyList<AdminDataTemplateDto> Templates =
    [
        new()
        {
            Id = "stable-household",
            Name = "Stable household, 7 months",
            Description = "Stable income, regular savings and mostly controlled spending. Good baseline for dashboard, analytics and forecasts.",
            FileName = "stable-household-7-months.json",
            Accounts = 5,
            Categories = 23,
            BudgetPlans = 7,
            Transactions = 371,
            FinancialGoals = 1
        },
        new()
        {
            Id = "declining-discipline",
            Name = "Declining discipline, 7 months",
            Description = "Income drops while discretionary overspend grows. Good for behavioral score, stability decline and auto-plan cuts.",
            FileName = "declining-discipline-7-months.json",
            Accounts = 5,
            Categories = 23,
            BudgetPlans = 7,
            Transactions = 368,
            FinancialGoals = 1
        },
        new()
        {
            Id = "seasonal-winter-holidays",
            Name = "Seasonal winter and holidays, 7 months",
            Description = "December gifts, winter utilities, spring normalization and an event budget. Good for seasonality checks.",
            FileName = "seasonal-winter-holidays-7-months.json",
            Accounts = 5,
            Categories = 23,
            BudgetPlans = 8,
            Transactions = 372,
            FinancialGoals = 1
        },
        new()
        {
            Id = "aggressive-goal-pressure",
            Name = "Aggressive goal pressure, 7 months",
            Description = "Strong savings goal pressure with reduced discretionary budgets. Good for goal-aware next-month plan generation.",
            FileName = "aggressive-goal-pressure-7-months.json",
            Accounts = 5,
            Categories = 23,
            BudgetPlans = 8,
            Transactions = 378,
            FinancialGoals = 2
        }
    ];

    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminDataManager(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
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
        var templatesDirectory = TryFindTemplatesDirectory();
        if (templatesDirectory == null)
            return Templates;

        return Templates
            .Where(template => File.Exists(Path.Combine(templatesDirectory, template.FileName)))
            .ToList();
    }

    public async Task<AdminDataImportDto> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken)
    {
        var template = Templates.FirstOrDefault(t =>
            string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase))
            ?? throw new CustomException($"Unknown admin data template: {templateId}", StatusCodes.Status404NotFound);

        var templatesDirectory = TryFindTemplatesDirectory()
            ?? throw new CustomException("Admin data templates directory was not found.", StatusCodes.Status404NotFound);
        var path = Path.Combine(templatesDirectory, template.FileName);
        if (!File.Exists(path))
            throw new CustomException($"Admin data template file was not found: {template.FileName}", StatusCodes.Status404NotFound);

        await using var stream = File.OpenRead(path);
        var dto = await JsonSerializer.DeserializeAsync<AdminDataImportDto>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return dto ?? throw new CustomException($"Admin data template is empty: {template.FileName}", StatusCodes.Status400BadRequest);
    }

    public AdminDataImportDto BuildSample()
    {
        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var data = new AdminDataSeedDto
        {
            Currencies =
            [
                new() { Key = "usd", Code = "USD", Title = "US Dollar", Symbol = "$", IsBase = true }
            ],
            Categories =
            [
                new() { Key = "salary", Title = "Salary", Type = "Income", Color = "#35B978" },
                new() { Key = "supermarket", Title = "Supermarket", Type = "Expense", Description = "Essential groceries", Color = "#35B978" },
                new() { Key = "utilities", Title = "Utilities", Type = "Expense", Description = "Heating, water, electricity", Color = "#4AA3FF" },
                new() { Key = "transport", Title = "Transport", Type = "Expense", Color = "#5FB3A7" },
                new() { Key = "entertainment", Title = "Entertainment", Type = "Expense", Color = "#A78BFA" },
                new() { Key = "delivery", Title = "Delivery food", Type = "Expense", Color = "#FF6B5F" },
                new() { Key = "shopping", Title = "Shopping", Type = "Expense", Color = "#F7B731" },
                new() { Key = "savings-transfer", Title = "Savings transfer", Type = "Transaction", Color = "#35B978" }
            ],
            Accounts =
            [
                new() { Key = "checking", Title = "Main checking", Amount = 1800m, CurrencyKey = "usd", Type = "Checking" },
                new() { Key = "cash", Title = "Cash", Amount = 300m, CurrencyKey = "usd", Type = "Cash" },
                new() { Key = "savings", Title = "Goal savings", Amount = 2400m, CurrencyKey = "usd", Type = "Savings" }
            ],
            FinancialGoals =
            [
                new()
                {
                    Title = "Emergency fund",
                    TargetAmount = 6000m,
                    InitialAmount = 1800m,
                    TargetDate = currentMonth.AddMonths(8).AddDays(14),
                    CreatedAt = currentMonth.AddMonths(-6),
                    LinkedAccountKey = "savings",
                    Description = "Safety buffer"
                }
            ]
        };

        for (var offset = -5; offset <= 0; offset++)
        {
            var month = currentMonth.AddMonths(offset);
            var planKey = $"plan-{month:yyyy-MM}";
            data.BudgetPlans.Add(new AdminBudgetPlanSeedDto
            {
                Key = planKey,
                Title = $"{month:MMMM yyyy} simulation",
                StartDate = month,
                EndDate = month.AddMonths(1).AddDays(-1),
                Type = "Monthly",
                Description = "Generated admin sample plan",
                Items =
                [
                    new() { CategoryKey = "supermarket", Amount = 900m, CurrencyKey = "usd" },
                    new() { CategoryKey = "utilities", Amount = offset is -5 or -4 or -3 ? 380m : 260m, CurrencyKey = "usd" },
                    new() { CategoryKey = "transport", Amount = 220m, CurrencyKey = "usd" },
                    new() { CategoryKey = "entertainment", Amount = offset >= -2 ? 450m : 300m, CurrencyKey = "usd" },
                    new() { CategoryKey = "delivery", Amount = offset >= -2 ? 360m : 180m, CurrencyKey = "usd" },
                    new() { CategoryKey = "shopping", Amount = offset >= -1 ? 500m : 250m, CurrencyKey = "usd" }
                ]
            });

            data.Transactions.Add(new AdminTransactionSeedDto
            {
                Title = $"Salary {month:yyyy-MM}",
                Amount = offset >= -2 ? 3000m : 3600m,
                Type = "Income",
                Date = month.AddDays(2),
                CurrencyKey = "usd",
                CategoryKey = "salary",
                AccountToKey = "checking"
            });

            AddExpenseMonth(data, planKey, month, offset);

            data.Transactions.Add(new AdminTransactionSeedDto
            {
                Title = $"Savings transfer {month:yyyy-MM}",
                Amount = offset >= -2 ? 150m : 450m,
                Type = "Transaction",
                Date = month.AddDays(5),
                CurrencyKey = "usd",
                CategoryKey = "savings-transfer",
                AccountFromKey = "checking",
                AccountToKey = "savings"
            });
        }

        return new AdminDataImportDto
        {
            ClearExisting = true,
            Data = data
        };
    }

    private static void AddExpenseMonth(AdminDataSeedDto data, string planKey, DateTime month, int offset)
    {
        var stress = offset >= -2 ? 1.35m : 1m;
        var essentials = offset >= -2 ? 1.08m : 1m;

        var expenses = new[]
        {
            ("Supermarket", "supermarket", 780m * essentials, 6),
            ("Utilities", "utilities", (offset is -5 or -4 or -3 ? 350m : 230m) * essentials, 8),
            ("Transport", "transport", 180m, 10),
            ("Entertainment", "entertainment", 240m * stress, 14),
            ("Delivery", "delivery", 145m * stress, 18),
            ("Shopping", "shopping", 220m * stress, 22)
        };

        foreach (var (title, categoryKey, amount, day) in expenses)
        {
            data.Transactions.Add(new AdminTransactionSeedDto
            {
                Title = $"{title} {month:yyyy-MM}",
                Amount = Math.Round(amount, 2),
                Type = "Expense",
                Date = month.AddDays(day),
                CurrencyKey = "usd",
                CategoryKey = categoryKey,
                BudgetPlanKey = planKey,
                AccountFromKey = "checking"
            });
        }
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
            await _context.SaveChangesAsync(cancellationToken);
            map[item.Key] = entity.Id;
            result.Categories++;
        }

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
            await _context.SaveChangesAsync(cancellationToken);
            map[item.Key] = entity.Id;
            result.Accounts++;
        }

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
            await _context.SaveChangesAsync(cancellationToken);
            map[item.Key] = entity.Id;
            planEntities[item.Key] = entity;
            result.BudgetPlans++;
        }

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

    private static string? TryFindTemplatesDirectory()
    {
        foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(root);
            while (current != null)
            {
                var candidates = new[]
                {
                    Path.Combine(current.FullName, "AdminDataTemplates"),
                    Path.Combine(current.FullName, "budget-tracker-backend", "AdminDataTemplates"),
                    Path.Combine(current.FullName, "budget-tracker-test-data", "admin-import")
                };

                foreach (var candidate in candidates)
                {
                    if (Directory.Exists(candidate))
                        return candidate;
                }

                current = current.Parent;
            }
        }

        return null;
    }
}
