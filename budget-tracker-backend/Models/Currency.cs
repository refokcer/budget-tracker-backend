namespace budget_tracker_backend.Models
{
    public class Currency
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Code { get; set; } = null!;
        public char Symbol { get; set; }
        public bool IsBase { get; set; }
    }
}
