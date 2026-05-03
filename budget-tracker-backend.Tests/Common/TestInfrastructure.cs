using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using budget_tracker_backend.Mapping;
using budget_tracker_backend.Mapping.Pages;
using budget_tracker_backend.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace budget_tracker_backend.Tests.Common;

internal static class TestInfrastructure
{
    public const string UserId = "test-user";
    public static readonly DateTime CurrentMonthStart = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime PreviousMonthStart = CurrentMonthStart.AddMonths(-1);

    public static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AccountProfile>();
            cfg.AddProfile<BudgetPlanItemProfile>();
            cfg.AddProfile<BudgetPlanProfile>();
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<CurrencyProfile>();
            cfg.AddProfile<FinancialGoalProfile>();
            cfg.AddProfile<TransactionProfile>();
            cfg.AddProfile<BudgetPlanPageProfile>();
            cfg.AddProfile<DashboardProfile>();
            cfg.AddProfile<ExpensesByMonthProfile>();
            cfg.AddProfile<IncomesByMonthProfile>();
            cfg.AddProfile<TransactionsFilteredProfile>();
            cfg.AddProfile<TransfersByMonthProfile>();
        });

        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }

    public static ApplicationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContext()
        };

        return new ApplicationDbContext(options, httpContextAccessor);
    }

    public static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(ClaimTypes.Name, "Unit Test User")
            ],
            authenticationType: "Test"));

        return context;
    }

    public static async Task SeedReferenceDataAsync(ApplicationDbContext context)
    {
        if (!await context.Users.AnyAsync(u => u.Id == UserId))
        {
            await context.Users.AddAsync(new ApplicationUser
            {
                Id = UserId,
                UserName = "unit@test.local",
                NormalizedUserName = "UNIT@TEST.LOCAL",
                Email = "unit@test.local",
                NormalizedEmail = "UNIT@TEST.LOCAL"
            });
        }

        if (await context.Currencies.AnyAsync())
        {
            await context.SaveChangesAsync(CancellationToken.None);
            return;
        }

        await context.Currencies.AddRangeAsync(
            new Currency { Id = 1, Title = "US Dollar", Code = "USD", Symbol = '$', IsBase = true },
            new Currency { Id = 2, Title = "Euro", Code = "EUR", Symbol = '€', IsBase = false });

        await context.Categories.AddRangeAsync(
            new Category { Id = 1, Title = "Salary", Type = TransactionCategoryType.Income, UserId = UserId },
            new Category { Id = 2, Title = "Groceries", Type = TransactionCategoryType.Expense, UserId = UserId },
            new Category { Id = 3, Title = "Transfer", Type = TransactionCategoryType.Transaction, UserId = UserId },
            new Category { Id = 4, Title = "Bonus", Type = TransactionCategoryType.Income, UserId = UserId });

        await context.Accounts.AddRangeAsync(
            new Account { Id = 1, Title = "Cash", Amount = 1000m, CurrencyId = 1, Type = AccountType.Cash, UserId = UserId },
            new Account { Id = 2, Title = "Savings", Amount = 500m, CurrencyId = 1, Type = AccountType.Savings, UserId = UserId },
            new Account { Id = 3, Title = "Travel", Amount = 200m, CurrencyId = 2, Type = AccountType.Investment, UserId = UserId });

        await context.BudgetPlans.AddRangeAsync(
            new BudgetPlan
            {
                Id = 1,
                Title = "March Plan",
                StartDate = CurrentMonthStart,
                EndDate = CurrentMonthStart.AddMonths(1).AddDays(-1),
                Type = BudgetPlanType.Monthly,
                Description = "Primary monthly plan",
                UserId = UserId
            },
            new BudgetPlan
            {
                Id = 2,
                Title = "Birthday Event",
                StartDate = CurrentMonthStart.AddDays(9),
                EndDate = CurrentMonthStart.AddDays(11),
                Type = BudgetPlanType.Event,
                Description = "Birthday weekend",
                ParentId = 1,
                UserId = UserId
            });

        await context.BudgetPlanItems.AddRangeAsync(
            new BudgetPlanItem { Id = 1, BudgetPlanId = 1, CategoryId = 2, Amount = 300m, CurrencyId = 1, Description = "Groceries cap" },
            new BudgetPlanItem { Id = 2, BudgetPlanId = 2, CategoryId = 2, Amount = 150m, CurrencyId = 1, Description = "Party food" });

        await context.Transactions.AddRangeAsync(
            new Transaction
            {
                Id = 1,
                Title = "Salary payment",
                Amount = 2500m,
                CategoryId = 1,
                CurrencyId = 1,
                Date = CurrentMonthStart.AddDays(4),
                Type = TransactionCategoryType.Income,
                AccountTo = 2,
                UserId = UserId,
                UnicCode = GenerateUnicCode(2500m, CurrentMonthStart.AddDays(4), null)
            },
            new Transaction
            {
                Id = 2,
                Title = "Weekly groceries",
                Amount = 120m,
                CategoryId = 2,
                CurrencyId = 1,
                BudgetPlanId = 1,
                Date = CurrentMonthStart.AddDays(6),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = UserId,
                UnicCode = GenerateUnicCode(120m, CurrentMonthStart.AddDays(6), null)
            },
            new Transaction
            {
                Id = 3,
                Title = "Move to savings",
                Amount = 300m,
                CategoryId = 3,
                CurrencyId = 1,
                Date = CurrentMonthStart.AddDays(7),
                Type = TransactionCategoryType.Transaction,
                AccountFrom = 1,
                AccountTo = 2,
                UserId = UserId,
                UnicCode = GenerateUnicCode(300m, CurrentMonthStart.AddDays(7), null)
            },
            new Transaction
            {
                Id = 4,
                Title = "Event groceries",
                Amount = 40m,
                CategoryId = 2,
                CurrencyId = 1,
                BudgetPlanId = 2,
                Date = CurrentMonthStart.AddDays(10),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = UserId,
                UnicCode = GenerateUnicCode(40m, CurrentMonthStart.AddDays(10), null)
            },
            new Transaction
            {
                Id = 5,
                Title = "February groceries",
                Amount = 30m,
                CategoryId = 2,
                CurrencyId = 1,
                Date = PreviousMonthStart.AddDays(24),
                Type = TransactionCategoryType.Expense,
                AccountFrom = 1,
                UserId = UserId,
                UnicCode = GenerateUnicCode(30m, PreviousMonthStart.AddDays(24), null)
            });

        await context.SaveChangesAsync(CancellationToken.None);
    }
    private static string GenerateUnicCode(decimal amount, DateTime date, string? authCode)
    {
        using var sha = SHA256.Create();
        var input = string.IsNullOrWhiteSpace(authCode)
            ? $"{amount}-{date:O}"
            : $"{amount}-{date:O}-{authCode}";
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

}
