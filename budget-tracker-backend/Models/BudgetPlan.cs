namespace budget_tracker_backend.Models
{
    public class BudgetPlan
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; }

        // Навигационные свойства
        public virtual Category Category { get; set; } = new Category();
    }
}
