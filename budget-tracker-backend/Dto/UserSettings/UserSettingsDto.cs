namespace budget_tracker_backend.Dto.UserSettings;

public class UserSettingsDto
{
    public int? DefaultCurrencyId { get; set; }
    public int? DefaultAccountId { get; set; }
    public AutoBudgetPlanRulesDto? AutoBudgetPlanRules { get; set; }
}

public class AutoBudgetPlanRulesDto
{
    public List<AutoBudgetPlanCategoryRuleDto> CategoryRules { get; set; } = new();
}

public class AutoBudgetPlanCategoryRuleDto
{
    public int CategoryId { get; set; }
    public decimal? MinimumLimit { get; set; }
    public decimal? MaximumLimit { get; set; }
    public string CutBehavior { get; set; } = "Normal";
    public List<AutoBudgetPlanMonthCoefficientDto> MonthCoefficients { get; set; } =
        AutoBudgetPlanRulesDefaults.CreateMonthCoefficients();
}

public class AutoBudgetPlanMonthCoefficientDto
{
    public int Month { get; set; }
    public decimal Multiplier { get; set; } = 1m;
}

public static class AutoBudgetPlanRulesDefaults
{
    public static List<AutoBudgetPlanMonthCoefficientDto> CreateMonthCoefficients()
    {
        return Enumerable.Range(1, 12)
            .Select(month => new AutoBudgetPlanMonthCoefficientDto
            {
                Month = month,
                Multiplier = 1m
            })
            .ToList();
    }
}
