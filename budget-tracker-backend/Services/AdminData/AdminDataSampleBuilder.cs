namespace budget_tracker_backend.Services.AdminData;

using budget_tracker_backend.Dto.AdminData;

public class AdminDataSampleBuilder : IAdminDataSampleBuilder
{
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
                new() { Key = "salary", Title = "Salary", Type = "Income", Priority = "Mandatory", Color = "#35B978" },
                new() { Key = "supermarket", Title = "Supermarket", Type = "Expense", Priority = "Mandatory", Description = "Essential groceries", Color = "#35B978" },
                new() { Key = "utilities", Title = "Utilities", Type = "Expense", Priority = "Mandatory", Description = "Heating, water, electricity", Color = "#4AA3FF" },
                new() { Key = "transport", Title = "Transport", Type = "Expense", Priority = "Flexible", Color = "#5FB3A7" },
                new() { Key = "entertainment", Title = "Entertainment", Type = "Expense", Priority = "Discretionary", Color = "#A78BFA" },
                new() { Key = "delivery", Title = "Delivery food", Type = "Expense", Priority = "Discretionary", Color = "#FF6B5F" },
                new() { Key = "shopping", Title = "Shopping", Type = "Expense", Priority = "Discretionary", Color = "#F7B731" },
                new() { Key = "savings-transfer", Title = "Savings transfer", Type = "Transfer", Priority = "Mandatory", Color = "#35B978" }
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

        data.RecurringPayments.AddRange([
            new()
            {
                Title = "Monthly salary",
                Amount = 3300m,
                Type = "Income",
                Frequency = "Monthly",
                DayOfMonth = 2,
                StartDate = currentMonth.AddMonths(-6),
                CurrencyKey = "usd",
                CategoryKey = "salary",
                AccountToKey = "checking",
                AutoCreateTransactions = true,
                Description = "Base salary recurrence"
            },
            new()
            {
                Title = "Rent",
                Amount = 950m,
                Type = "Expense",
                Frequency = "Monthly",
                DayOfMonth = 3,
                StartDate = currentMonth.AddMonths(-6),
                CurrencyKey = "usd",
                CategoryKey = "utilities",
                AccountFromKey = "checking",
                AutoCreateTransactions = true,
                Description = "Fixed housing payment"
            },
            new()
            {
                Title = "Streaming subscriptions",
                Amount = 38m,
                Type = "Expense",
                Frequency = "Monthly",
                DayOfMonth = 12,
                StartDate = currentMonth.AddMonths(-6),
                CurrencyKey = "usd",
                CategoryKey = "entertainment",
                AccountFromKey = "checking",
                Description = "Recurring subscriptions"
            }
        ]);

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
                Type = "Transfer",
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
}


