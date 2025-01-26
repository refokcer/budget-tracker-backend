namespace budget_tracker_backend.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
    }
}
