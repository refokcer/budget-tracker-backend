namespace budget_tracker_backend.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string BudgetPlanId { get; set; } = null!;
        public string? Description { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction> { };

        // Навигационные свойства
        public virtual BudgetPlan BudgetPlan { get; set; } = null!;
    }
}
