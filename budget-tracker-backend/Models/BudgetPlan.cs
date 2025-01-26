namespace budget_tracker_backend.Models
{
    public class BudgetPlan
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public int CurrencyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; } = null!; // enum of monthly or event
        public string? Description { get; set; }

        // Навигационные свойства
        public virtual Category Category { get; set; } = new Category();
    }
}
