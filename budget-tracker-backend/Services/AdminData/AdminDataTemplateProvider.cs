namespace budget_tracker_backend.Services.AdminData;

using System.Text.Json;
using budget_tracker_backend.Dto.AdminData;
using budget_tracker_backend.Exceptions;
using Microsoft.AspNetCore.Http;

public class AdminDataTemplateProvider : IAdminDataTemplateProvider
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
