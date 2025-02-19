namespace budget_tracker_backend.Dto.Categories;

public class CategoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int Type { get; set; } // Enum or int
    public string? Description { get; set; }
}