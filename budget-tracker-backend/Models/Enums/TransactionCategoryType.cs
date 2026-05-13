namespace budget_tracker_backend.Models.Enums;

public enum TransactionCategoryType
{
    Transfer = 0,

    [Obsolete("Use Transfer for account-to-account money movement.")]
    Transaction = Transfer,

    Income,
    Expense,
    None
}
