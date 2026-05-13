using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Dto.RecurringPayments;

public class RecurringPaymentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencySymbol { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryTitle { get; set; }
    public string? CategoryColor { get; set; }
    public int? AccountFrom { get; set; }
    public string? AccountFromTitle { get; set; }
    public int? AccountTo { get; set; }
    public string? AccountToTitle { get; set; }
    public TransactionCategoryType Type { get; set; }
    public RecurringPaymentFrequency Frequency { get; set; }
    public int Interval { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool AutoCreateTransactions { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public string? Description { get; set; }
    public List<RecurringPaymentOccurrenceDto> Preview { get; set; } = new();
}

public class CreateRecurringPaymentDto
{
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
    public int? AccountFrom { get; set; }
    public int? AccountTo { get; set; }
    public TransactionCategoryType Type { get; set; }
    public RecurringPaymentFrequency Frequency { get; set; } = RecurringPaymentFrequency.Monthly;
    public int Interval { get; set; } = 1;
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoCreateTransactions { get; set; }
    public string? Description { get; set; }
}

public class UpdateRecurringPaymentDto : CreateRecurringPaymentDto
{
    public int Id { get; set; }
}

public class RecurringPaymentOccurrenceDto
{
    public int RecurringPaymentId { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public TransactionCategoryType Type { get; set; }
    public int CurrencyId { get; set; }
    public int? CategoryId { get; set; }
}

public class GenerateRecurringPaymentsDto
{
    public DateTime? UpTo { get; set; }
}

public class GenerateRecurringPaymentsResultDto
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public List<RecurringPaymentOccurrenceDto> Transactions { get; set; } = new();
}

public class RecurringPaymentOptionsDto
{
    public List<RecurringPaymentOptionDto> Currencies { get; set; } = new();
    public List<RecurringPaymentOptionDto> Accounts { get; set; } = new();
    public List<RecurringPaymentCategoryOptionDto> Categories { get; set; } = new();
}

public class RecurringPaymentOptionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Code { get; set; }
    public string? Symbol { get; set; }
}

public class RecurringPaymentCategoryOptionDto : RecurringPaymentOptionDto
{
    public TransactionCategoryType Type { get; set; }
    public string? Color { get; set; }
}
