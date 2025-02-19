namespace budget_tracker_backend.Dto.Categories;

public class CreateCategoryDto
{
    public string Title { get; set; } = null!;
    public int Type { get; set; }
    public string? Description { get; set; }
}
