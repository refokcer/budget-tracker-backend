using budget_tracker_backend.Models;

namespace budget_tracker_backend.Services.RecurringPayments;

public static class RecurringPaymentSchedule
{
    public static List<DateTime> GetOccurrences(
        RecurringPayment payment,
        DateTime startInclusive,
        DateTime endExclusive)
    {
        var rangeStart = startInclusive.Date;
        var rangeEndExclusive = endExclusive.Date;
        var paymentStart = payment.StartDate.Date;
        var paymentEndExclusive = payment.EndDate?.Date.AddDays(1);

        if (!payment.IsActive || payment.Interval <= 0 || paymentStart >= rangeEndExclusive)
            return [];

        if (paymentEndExclusive.HasValue && paymentEndExclusive.Value <= rangeStart)
            return [];

        var effectiveStart = paymentStart > rangeStart ? paymentStart : rangeStart;
        var effectiveEnd = paymentEndExclusive.HasValue && paymentEndExclusive.Value < rangeEndExclusive
            ? paymentEndExclusive.Value
            : rangeEndExclusive;

        return payment.Frequency switch
        {
            Models.Enums.RecurringPaymentFrequency.Weekly =>
                GetWeeklyOccurrences(payment, effectiveStart, effectiveEnd),
            Models.Enums.RecurringPaymentFrequency.Yearly =>
                GetYearlyOccurrences(payment, effectiveStart, effectiveEnd),
            _ => GetMonthlyOccurrences(payment, effectiveStart, effectiveEnd)
        };
    }

    private static List<DateTime> GetMonthlyOccurrences(
        RecurringPayment payment,
        DateTime startInclusive,
        DateTime endExclusive)
    {
        var result = new List<DateTime>();
        var anchor = new DateTime(payment.StartDate.Year, payment.StartDate.Month, 1);
        var cursor = new DateTime(startInclusive.Year, startInclusive.Month, 1);
        var day = payment.DayOfMonth ?? payment.StartDate.Day;

        while (cursor < endExclusive)
        {
            var monthsDiff = ((cursor.Year - anchor.Year) * 12) + cursor.Month - anchor.Month;
            if (monthsDiff >= 0 && monthsDiff % payment.Interval == 0)
            {
                var occurrenceDay = Math.Min(Math.Max(1, day), DateTime.DaysInMonth(cursor.Year, cursor.Month));
                var occurrence = new DateTime(cursor.Year, cursor.Month, occurrenceDay);
                if (occurrence >= startInclusive && occurrence < endExclusive && occurrence >= payment.StartDate.Date)
                    result.Add(occurrence);
            }

            cursor = cursor.AddMonths(1);
        }

        return result;
    }

    private static List<DateTime> GetWeeklyOccurrences(
        RecurringPayment payment,
        DateTime startInclusive,
        DateTime endExclusive)
    {
        var result = new List<DateTime>();
        var targetDay = payment.DayOfWeek ?? payment.StartDate.DayOfWeek;
        var cursor = startInclusive;
        var daysToTarget = ((int)targetDay - (int)cursor.DayOfWeek + 7) % 7;
        cursor = cursor.AddDays(daysToTarget);

        while (cursor < endExclusive)
        {
            var weeksDiff = (int)((cursor.Date - payment.StartDate.Date).TotalDays / 7);
            if (weeksDiff >= 0 && weeksDiff % payment.Interval == 0)
                result.Add(cursor);

            cursor = cursor.AddDays(7);
        }

        return result;
    }

    private static List<DateTime> GetYearlyOccurrences(
        RecurringPayment payment,
        DateTime startInclusive,
        DateTime endExclusive)
    {
        var result = new List<DateTime>();
        var cursorYear = startInclusive.Year;
        var targetMonth = payment.StartDate.Month;
        var targetDay = payment.DayOfMonth ?? payment.StartDate.Day;

        while (new DateTime(cursorYear, 1, 1) < endExclusive)
        {
            var yearsDiff = cursorYear - payment.StartDate.Year;
            if (yearsDiff >= 0 && yearsDiff % payment.Interval == 0)
            {
                var occurrenceDay = Math.Min(Math.Max(1, targetDay), DateTime.DaysInMonth(cursorYear, targetMonth));
                var occurrence = new DateTime(cursorYear, targetMonth, occurrenceDay);
                if (occurrence >= startInclusive && occurrence < endExclusive && occurrence >= payment.StartDate.Date)
                    result.Add(occurrence);
            }

            cursorYear++;
        }

        return result;
    }
}
