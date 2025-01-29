namespace budget_tracker_backend.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Type { get; set; } = null!; // Enum of => Income, Expense
        public string? Description { get; set; }
    }
}
