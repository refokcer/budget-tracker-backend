using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.RecurringPayments;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.Services.RecurringPayments;

public class RecurringPaymentManager : IRecurringPaymentManager
{
    private readonly IApplicationDbContext _context;
    private readonly ITransactionManager _transactionManager;

    public RecurringPaymentManager(IApplicationDbContext context, ITransactionManager transactionManager)
    {
        _context = context;
        _transactionManager = transactionManager;
    }

    public async Task<IEnumerable<RecurringPaymentDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var payments = await QueryDetailed()
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Type)
            .ThenBy(p => p.Title)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var previewEnd = DateTime.UtcNow.Date.AddMonths(3);
        return payments.Select(p => ToDto(p, DateTime.UtcNow.Date, previewEnd));
    }

    public async Task<RecurringPaymentDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var payment = await QueryDetailed()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment == null)
            throw new CustomException("Recurring payment not found", StatusCodes.Status404NotFound);

        return ToDto(payment, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddMonths(3));
    }

    public async Task<RecurringPaymentDto> CreateAsync(CreateRecurringPaymentDto dto, CancellationToken cancellationToken)
    {
        await ValidateAsync(dto, cancellationToken);

        var entity = new RecurringPayment();
        ApplyDto(entity, dto);

        await _context.RecurringPayments.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create recurring payment", StatusCodes.Status500InternalServerError);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<RecurringPaymentDto> UpdateAsync(UpdateRecurringPaymentDto dto, CancellationToken cancellationToken)
    {
        var entity = await _context.RecurringPayments
            .FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken);

        if (entity == null)
            throw new CustomException("Recurring payment not found", StatusCodes.Status404NotFound);

        await ValidateAsync(dto, cancellationToken);
        ApplyDto(entity, dto);

        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update recurring payment", StatusCodes.Status500InternalServerError);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.RecurringPayments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity == null)
            throw new CustomException("Recurring payment not found", StatusCodes.Status404NotFound);

        _context.RecurringPayments.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete recurring payment", StatusCodes.Status500InternalServerError);

        return true;
    }

    public async Task<RecurringPaymentOptionsDto> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var currencies = await _context.Currencies
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new RecurringPaymentOptionDto
            {
                Id = c.Id,
                Title = c.Title,
                Code = c.Code,
                Symbol = c.Symbol.ToString()
            })
            .ToListAsync(cancellationToken);

        var accounts = await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Title)
            .Select(a => new RecurringPaymentOptionDto
            {
                Id = a.Id,
                Title = a.Title
            })
            .ToListAsync(cancellationToken);

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Title)
            .Select(c => new RecurringPaymentCategoryOptionDto
            {
                Id = c.Id,
                Title = c.Title,
                Type = c.Type,
                Color = c.Color
            })
            .ToListAsync(cancellationToken);

        return new RecurringPaymentOptionsDto
        {
            Currencies = currencies,
            Accounts = accounts,
            Categories = categories
        };
    }

    public async Task<List<RecurringPaymentOccurrenceDto>> GetProjectedOccurrencesAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var payments = await QueryDetailed()
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.StartDate < endExclusive
                && (p.EndDate == null || p.EndDate >= startInclusive))
            .ToListAsync(cancellationToken);

        return payments
            .SelectMany(p => RecurringPaymentSchedule.GetOccurrences(p, startInclusive, endExclusive)
                .Select(date => ToOccurrenceDto(p, date)))
            .OrderBy(o => o.Date)
            .ToList();
    }

    public async Task<GenerateRecurringPaymentsResultDto> GenerateDueTransactionsAsync(
        DateTime upTo,
        CancellationToken cancellationToken)
    {
        var endExclusive = upTo.Date.AddDays(1);
        var payments = await _context.RecurringPayments
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.AutoCreateTransactions
                && p.StartDate < endExclusive)
            .ToListAsync(cancellationToken);

        var result = new GenerateRecurringPaymentsResultDto();

        foreach (var payment in payments)
        {
            var start = payment.LastGeneratedDate?.Date.AddDays(1) ?? payment.StartDate.Date;
            var occurrences = RecurringPaymentSchedule.GetOccurrences(payment, start, endExclusive);

            foreach (var date in occurrences)
            {
                var authCode = BuildAuthCode(payment.Id, date);
                var exists = await _context.Transactions
                    .AsNoTracking()
                    .AnyAsync(t => t.AuthCode == authCode, cancellationToken);

                if (exists)
                {
                    result.Skipped++;
                    continue;
                }

                var budgetPlanId = payment.Type == TransactionCategoryType.Expense
                    ? await FindMonthlyPlanIdAsync(date, cancellationToken)
                    : null;

                await _transactionManager.CreateAsync(new CreateTransactionDto
                {
                    Title = payment.Title,
                    Amount = payment.Amount,
                    AccountFrom = payment.AccountFrom,
                    AccountTo = payment.AccountTo,
                    BudgetPlanId = budgetPlanId,
                    CurrencyId = payment.CurrencyId,
                    CategoryId = payment.CategoryId,
                    Date = date,
                    Type = payment.Type,
                    Description = payment.Description,
                    AuthCode = authCode
                }, cancellationToken);

                result.Created++;
                result.Transactions.Add(ToOccurrenceDto(payment, date));
            }

            if (occurrences.Count > 0)
            {
                var tracked = await _context.RecurringPayments
                    .FirstAsync(p => p.Id == payment.Id, cancellationToken);
                tracked.LastGeneratedDate = occurrences.Max();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return result;
    }

    private IQueryable<RecurringPayment> QueryDetailed()
    {
        return _context.RecurringPayments
            .Include(p => p.Currency)
            .Include(p => p.Category)
            .Include(p => p.FromAccount)
            .Include(p => p.ToAccount);
    }

    private async Task ValidateAsync(CreateRecurringPaymentDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new CustomException("Title is required", StatusCodes.Status400BadRequest);

        if (dto.Amount <= 0m)
            throw new CustomException("Amount must be greater than zero", StatusCodes.Status400BadRequest);

        if (dto.Interval < 1)
            throw new CustomException("Interval must be at least 1", StatusCodes.Status400BadRequest);

        if (dto.Type == TransactionCategoryType.None)
            throw new CustomException("Recurring payment type is required", StatusCodes.Status400BadRequest);

        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
            throw new CustomException("End date cannot be before start date", StatusCodes.Status400BadRequest);

        if (dto.DayOfMonth is < 1 or > 31)
            throw new CustomException("Day of month must be between 1 and 31", StatusCodes.Status400BadRequest);

        if (!await _context.Currencies.AsNoTracking().AnyAsync(c => c.Id == dto.CurrencyId, cancellationToken))
            throw new CustomException("Currency not found", StatusCodes.Status400BadRequest);

        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value, cancellationToken);

            if (category == null)
                throw new CustomException("Category not found", StatusCodes.Status400BadRequest);

            if (category.Type != dto.Type)
                throw new CustomException("Category type does not match recurring payment type", StatusCodes.Status400BadRequest);
        }

        if (dto.AccountFrom.HasValue
            && !await _context.Accounts.AsNoTracking().AnyAsync(a => a.Id == dto.AccountFrom.Value, cancellationToken))
            throw new CustomException("AccountFrom not found", StatusCodes.Status400BadRequest);

        if (dto.AccountTo.HasValue
            && !await _context.Accounts.AsNoTracking().AnyAsync(a => a.Id == dto.AccountTo.Value, cancellationToken))
            throw new CustomException("AccountTo not found", StatusCodes.Status400BadRequest);
    }

    private static void ApplyDto(RecurringPayment entity, CreateRecurringPaymentDto dto)
    {
        entity.Title = dto.Title.Trim();
        entity.Amount = dto.Amount;
        entity.CurrencyId = dto.CurrencyId;
        entity.CategoryId = dto.CategoryId;
        entity.AccountFrom = dto.AccountFrom;
        entity.AccountTo = dto.AccountTo;
        entity.Type = dto.Type;
        entity.Frequency = dto.Frequency;
        entity.Interval = Math.Max(1, dto.Interval);
        entity.DayOfMonth = dto.DayOfMonth;
        entity.DayOfWeek = dto.DayOfWeek;
        entity.StartDate = dto.StartDate.Date;
        entity.EndDate = dto.EndDate?.Date;
        entity.IsActive = dto.IsActive;
        entity.AutoCreateTransactions = dto.AutoCreateTransactions;
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
    }

    private static RecurringPaymentDto ToDto(RecurringPayment payment, DateTime previewStart, DateTime previewEnd)
    {
        return new RecurringPaymentDto
        {
            Id = payment.Id,
            Title = payment.Title,
            Amount = payment.Amount,
            CurrencyId = payment.CurrencyId,
            CurrencyCode = payment.Currency?.Code,
            CurrencySymbol = payment.Currency?.Symbol.ToString(),
            CategoryId = payment.CategoryId,
            CategoryTitle = payment.Category?.Title,
            CategoryColor = payment.Category?.Color,
            AccountFrom = payment.AccountFrom,
            AccountFromTitle = payment.FromAccount?.Title,
            AccountTo = payment.AccountTo,
            AccountToTitle = payment.ToAccount?.Title,
            Type = payment.Type,
            Frequency = payment.Frequency,
            Interval = payment.Interval,
            DayOfMonth = payment.DayOfMonth,
            DayOfWeek = payment.DayOfWeek,
            StartDate = payment.StartDate,
            EndDate = payment.EndDate,
            IsActive = payment.IsActive,
            AutoCreateTransactions = payment.AutoCreateTransactions,
            LastGeneratedDate = payment.LastGeneratedDate,
            Description = payment.Description,
            Preview = RecurringPaymentSchedule.GetOccurrences(payment, previewStart, previewEnd)
                .Take(3)
                .Select(date => ToOccurrenceDto(payment, date))
                .ToList()
        };
    }

    private static RecurringPaymentOccurrenceDto ToOccurrenceDto(RecurringPayment payment, DateTime date)
    {
        return new RecurringPaymentOccurrenceDto
        {
            RecurringPaymentId = payment.Id,
            Title = payment.Title,
            Amount = payment.Amount,
            Date = date,
            Type = payment.Type,
            CurrencyId = payment.CurrencyId,
            CategoryId = payment.CategoryId
        };
    }

    private async Task<int?> FindMonthlyPlanIdAsync(DateTime date, CancellationToken cancellationToken)
    {
        return await _context.BudgetPlans
            .AsNoTracking()
            .Where(p => p.Type == BudgetPlanType.Monthly
                && p.StartDate <= date
                && p.EndDate >= date)
            .OrderByDescending(p => p.StartDate)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string BuildAuthCode(int paymentId, DateTime date)
    {
        return $"recurring:{paymentId}:{date:yyyyMMdd}";
    }
}
