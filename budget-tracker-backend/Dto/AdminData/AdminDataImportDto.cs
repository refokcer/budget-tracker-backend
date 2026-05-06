namespace budget_tracker_backend.Dto.AdminData;

public class AdminDataImportDto
{
    public bool ClearExisting { get; set; }
    public AdminDataSeedDto Data { get; set; } = new();
}

public class AdminDataSeedDto
{
    public List<AdminCurrencySeedDto> Currencies { get; set; } = new();
    public List<AdminCategorySeedDto> Categories { get; set; } = new();
    public List<AdminAccountSeedDto> Accounts { get; set; } = new();
    public List<AdminBudgetPlanSeedDto> BudgetPlans { get; set; } = new();
    public List<AdminTransactionSeedDto> Transactions { get; set; } = new();
    public List<AdminFinancialGoalSeedDto> FinancialGoals { get; set; } = new();
}

public class AdminCurrencySeedDto
{
    public string Key { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public bool IsBase { get; set; }
}

public class AdminCategorySeedDto
{
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public class AdminAccountSeedDto
{
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencyKey { get; set; } = null!;
    public string Type { get; set; } = "Other";
    public string? Description { get; set; }
}

public class AdminBudgetPlanSeedDto
{
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = "Monthly";
    public string? ParentKey { get; set; }
    public string? Description { get; set; }
    public List<AdminBudgetPlanItemSeedDto> Items { get; set; } = new();
}

public class AdminBudgetPlanItemSeedDto
{
    public string CategoryKey { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencyKey { get; set; } = null!;
    public string? Description { get; set; }
}

public class AdminTransactionSeedDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Type { get; set; } = null!;
    public DateTime Date { get; set; }
    public string CurrencyKey { get; set; } = null!;
    public string? CategoryKey { get; set; }
    public string? BudgetPlanKey { get; set; }
    public string? AccountFromKey { get; set; }
    public string? AccountToKey { get; set; }
    public string? Description { get; set; }
    public string? AuthCode { get; set; }
    public string? UnicCode { get; set; }
}

public class AdminFinancialGoalSeedDto
{
    public string Title { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal InitialAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? LinkedAccountKey { get; set; }
    public string? Description { get; set; }
}

public class AdminDataImportResultDto
{
    public int Currencies { get; set; }
    public int Categories { get; set; }
    public int Accounts { get; set; }
    public int BudgetPlans { get; set; }
    public int BudgetPlanItems { get; set; }
    public int Transactions { get; set; }
    public int FinancialGoals { get; set; }
}

public class AdminDataTemplateDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public int Accounts { get; set; }
    public int Categories { get; set; }
    public int BudgetPlans { get; set; }
    public int Transactions { get; set; }
    public int FinancialGoals { get; set; }
}
