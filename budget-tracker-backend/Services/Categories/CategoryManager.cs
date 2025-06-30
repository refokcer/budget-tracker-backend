namespace budget_tracker_backend.Services.Categories;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

public class CategoryManager : ICategoryManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CategoryManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetByTypeAsync(TransactionCategoryType type, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(c => c.Type == type)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Category> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Category>(dto) ??
            throw new CustomException("Cannot map CreateCategoryDto", StatusCodes.Status400BadRequest);

        await _context.Categories.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create category", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<Category> UpdateAsync(CategoryDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.Categories.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Category not found", StatusCodes.Status404NotFound);

        _mapper.Map(dto, existing);
        _context.Categories.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update category", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            throw new CustomException("Category not found", StatusCodes.Status404NotFound);

        _context.Categories.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete category", StatusCodes.Status500InternalServerError);

        return true;
    }
}
