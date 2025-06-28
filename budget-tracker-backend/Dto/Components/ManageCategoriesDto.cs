namespace budget_tracker_backend.Dto.Components;

using budget_tracker_backend.Dto.Categories;

/// <summary>
/// Data for manage categories component.
/// </summary>
public class ManageCategoriesDto
{
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
}

