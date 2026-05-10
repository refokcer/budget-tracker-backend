namespace budget_tracker_backend.Services.Algorithms;

using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

internal static class AlgorithmHelpers
{
    public static decimal Clamp(decimal value)
    {
        return Math.Min(1m, Math.Max(0m, value));
    }

    public static bool IsSavingsAccount(Account account)
    {
        return account.Type is AccountType.Savings
            or AccountType.Deposit
            or AccountType.Investment;
    }
}
