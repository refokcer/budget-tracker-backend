namespace budget_tracker_backend.Services.Categories;

using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

public interface ICategoryManager
{
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<Category>> GetByTypeAsync(TransactionCategoryType type, CancellationToken cancellationToken);
    Task<Category> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken);
    Task<Category> UpdateAsync(CategoryDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
