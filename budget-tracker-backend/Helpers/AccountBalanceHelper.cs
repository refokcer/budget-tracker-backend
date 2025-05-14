using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Helpers;

public static class AccountBalanceHelper
{
    public static void Apply(
        TransactionCategoryType type,
        decimal amount,
        Account? from,            // может быть null
        Account? to,              // может быть null
        bool reverse = false)     // true → отменяем, false → применяем
    {
        var sign = reverse ? -1 : 1;

        switch (type)
        {
            case TransactionCategoryType.Income:
                if (to != null) to.Amount += sign * amount;
                break;

            case TransactionCategoryType.Expense:
                if (from != null) from.Amount -= sign * amount;
                break;

            case TransactionCategoryType.Transaction:
                if (from != null) from.Amount -= sign * amount;
                if (to != null) to.Amount += sign * amount;
                break;
        }
    }
}
