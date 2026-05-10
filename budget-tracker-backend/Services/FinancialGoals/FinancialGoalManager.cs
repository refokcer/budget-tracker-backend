namespace budget_tracker_backend.Services.FinancialGoals;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Algorithms.FinancialGoals;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class FinancialGoalManager : IFinancialGoalManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IFinancialGoalForecastAlgorithm _forecastAlgorithm;
    private readonly IFinancialGoalBudgetAdjustmentAlgorithm _budgetAdjustmentAlgorithm;

    public FinancialGoalManager(
        IApplicationDbContext context,
        IMapper mapper,
        IFinancialGoalForecastAlgorithm forecastAlgorithm,
        IFinancialGoalBudgetAdjustmentAlgorithm budgetAdjustmentAlgorithm)
    {
        _context = context;
        _mapper = mapper;
        _forecastAlgorithm = forecastAlgorithm;
        _budgetAdjustmentAlgorithm = budgetAdjustmentAlgorithm;
    }

    public async Task<IEnumerable<FinancialGoal>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .AsNoTracking()
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialGoal?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<FinancialGoal> CreateAsync(CreateFinancialGoalDto dto, CancellationToken cancellationToken)
    {
        ValidateGoalDto(dto.Title, dto.TargetAmount, dto.InitialAmount, dto.TargetDate);
        await ValidateLinkedAccountAsync(dto.LinkedAccountId, cancellationToken);

        var entity = _mapper.Map<FinancialGoal>(dto)
            ?? throw new CustomException("Cannot map CreateFinancialGoalDto", StatusCodes.Status400BadRequest);

        entity.CreatedAt = DateTime.UtcNow;

        await _context.FinancialGoals.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create financial goal", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<FinancialGoal> UpdateAsync(FinancialGoalDto dto, CancellationToken cancellationToken)
    {
        ValidateGoalDto(dto.Title, dto.TargetAmount, dto.InitialAmount, dto.TargetDate);
        await ValidateLinkedAccountAsync(dto.LinkedAccountId, cancellationToken);

        var existing = await _context.FinancialGoals.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);

        existing.Title = dto.Title;
        existing.TargetAmount = dto.TargetAmount;
        existing.InitialAmount = dto.InitialAmount;
        existing.TargetDate = dto.TargetDate;
        existing.LinkedAccountId = dto.LinkedAccountId;
        existing.Description = dto.Description;

        _context.FinancialGoals.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update financial goal", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.FinancialGoals.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);

        _context.FinancialGoals.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete financial goal", StatusCodes.Status500InternalServerError);

        return true;
    }

    public async Task<FinancialGoalForecastDto> GetForecastAsync(int id, CancellationToken cancellationToken)
    {
        var goal = await GetRequiredGoalAsync(id, cancellationToken);
        return await _forecastAlgorithm.CalculateAsync(goal, cancellationToken);
    }

    public async Task<ApplyBudgetAdjustmentsResultDto> ApplyBudgetAdjustmentsAsync(
        int id,
        ApplyBudgetAdjustmentsRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var goal = await GetRequiredGoalAsync(id, cancellationToken);
        var forecast = await _forecastAlgorithm.CalculateAsync(goal, cancellationToken);
        var hasCustomAdjustments = dto?.Adjustments != null;

        if (!hasCustomAdjustments && forecast.SuggestedBudgetAdjustments.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = null,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentMonthEnd = currentMonthStart.AddMonths(1);

        var activePlan = await _budgetAdjustmentAlgorithm.GetActiveMonthlyPlanAsync(
            currentMonthStart,
            currentMonthEnd,
            cancellationToken);
        if (activePlan == null)
            throw new CustomException("No active monthly budget plan found for adaptive adjustments", StatusCodes.Status400BadRequest);

        var adjustmentsToApply = hasCustomAdjustments
            ? _budgetAdjustmentAlgorithm.ResolveRequestedAdjustments(forecast.SuggestedBudgetAdjustments, dto!.Adjustments!)
            : forecast.SuggestedBudgetAdjustments;

        if (adjustmentsToApply.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = activePlan.Id,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var appliedSuggestions = new List<BudgetAdjustmentSuggestionDto>();
        foreach (var suggestion in adjustmentsToApply)
        {
            var existingItem = activePlan.Items?.FirstOrDefault(i => i.CategoryId == suggestion.CategoryId);
            if (existingItem == null)
                continue;

            if (await _budgetAdjustmentAlgorithm.HasAppliedAdjustmentAsync(activePlan.UserId, activePlan.Id, suggestion.CategoryId, cancellationToken))
                continue;

            existingItem.Amount = suggestion.RecommendedBudgetLimit;
            await _context.UserClaims.AddAsync(new Microsoft.AspNetCore.Identity.IdentityUserClaim<string>
            {
                UserId = activePlan.UserId,
                ClaimType = _budgetAdjustmentAlgorithm.GetAppliedAdjustmentClaimType(activePlan.Id, suggestion.CategoryId),
                ClaimValue = $"{goal.Id}|{suggestion.SuggestedReduction:0.##}|{DateTime.UtcNow:O}"
            }, cancellationToken);

            appliedSuggestions.Add(suggestion);
        }

        if (appliedSuggestions.Count == 0)
        {
            return new ApplyBudgetAdjustmentsResultDto
            {
                GoalId = id,
                BudgetPlanId = activePlan.Id,
                AppliedAdjustmentsCount = 0,
                Forecast = forecast
            };
        }

        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to apply adaptive budget adjustments", StatusCodes.Status500InternalServerError);

        return new ApplyBudgetAdjustmentsResultDto
        {
            GoalId = id,
            BudgetPlanId = activePlan.Id,
            AppliedAdjustmentsCount = appliedSuggestions.Count,
            AppliedAdjustments = appliedSuggestions,
            Forecast = forecast
        };
    }

    private async Task<FinancialGoal> GetRequiredGoalAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.FinancialGoals
            .Include(g => g.LinkedAccount)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);
    }

    private async Task ValidateLinkedAccountAsync(int? linkedAccountId, CancellationToken cancellationToken)
    {
        if (!linkedAccountId.HasValue)
            return;

        var linkedAccount = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == linkedAccountId.Value, cancellationToken);
        if (linkedAccount == null)
            throw new CustomException("Linked account not found", StatusCodes.Status400BadRequest);

        if (!IsSavingsAccount(linkedAccount))
            throw new CustomException("Linked account must be a savings, deposit, or investment account", StatusCodes.Status400BadRequest);
    }

    private static void ValidateGoalDto(string title, decimal targetAmount, decimal initialAmount, DateTime targetDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new CustomException("Financial goal title is required", StatusCodes.Status400BadRequest);

        if (targetAmount <= 0m)
            throw new CustomException("Target amount must be greater than zero", StatusCodes.Status400BadRequest);

        if (initialAmount < 0m)
            throw new CustomException("Initial amount cannot be negative", StatusCodes.Status400BadRequest);

        if (initialAmount > targetAmount)
            throw new CustomException("Initial amount cannot exceed target amount", StatusCodes.Status400BadRequest);

        if (targetDate.Date <= DateTime.UtcNow.Date)
            throw new CustomException("Target date must be in the future", StatusCodes.Status400BadRequest);
    }

    private static bool IsSavingsAccount(Account account)
    {
        return account.Type is AccountType.Savings
            or AccountType.Deposit
            or AccountType.Investment;
    }
}
