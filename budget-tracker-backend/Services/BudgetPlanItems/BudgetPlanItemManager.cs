namespace budget_tracker_backend.Services.BudgetPlanItems;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class BudgetPlanItemManager : IBudgetPlanItemManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BudgetPlanItemManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BudgetPlanItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.BudgetPlanItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetPlanItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.BudgetPlanItems.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<BudgetPlanItem>> GetByPlanIdAsync(int planId, CancellationToken cancellationToken)
    {
        return await _context.BudgetPlanItems
            .Include(i => i.Category)
            .Include(i => i.Currency)
            .Where(i => i.BudgetPlanId == planId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetPlanItem> CreateAsync(CreateBudgetPlanItemDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<BudgetPlanItem>(dto) ??
            throw new CustomException("Cannot map CreateBudgetPlanItemDto", StatusCodes.Status400BadRequest);
        entity.CategoryId = await ResolveCategoryIdAsync(dto.CategoryId, cancellationToken);

        await _context.BudgetPlanItems.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create plan item", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<BudgetPlanItem> UpdateAsync(BudgetPlanItemDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.BudgetPlanItems.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Budget plan item not found", StatusCodes.Status404NotFound);

        existing.BudgetPlanId = dto.BudgetPlanId;
        existing.CategoryId = await ResolveCategoryIdAsync(dto.CategoryId, cancellationToken);
        existing.Amount = dto.Amount;
        existing.CurrencyId = dto.CurrencyId;
        existing.Description = dto.Description;

        _context.BudgetPlanItems.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update plan item", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var item = await _context.BudgetPlanItems.FindAsync(new object[] { id }, cancellationToken);
        if (item == null)
            throw new CustomException("Budget plan item not found", StatusCodes.Status404NotFound);

        _context.BudgetPlanItems.Remove(item);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete plan item", StatusCodes.Status500InternalServerError);

        return true;
    }

    private async Task<int> ResolveCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (categoryId > 0)
            return categoryId;

        var otherCategory = await _context.Categories
            .FirstOrDefaultAsync(
                c => c.Type == TransactionCategoryType.Expense
                    && (c.Title == "Other" || c.Title == "Others" || c.Title == "Другие" || c.Title == "Інше"),
                cancellationToken);

        if (otherCategory != null)
            return otherCategory.Id;

        otherCategory = new Category
        {
            Title = "Other",
            Type = TransactionCategoryType.Expense,
            Description = "Budget bucket for expenses from categories not explicitly included in a plan"
        };

        await _context.Categories.AddAsync(otherCategory, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create Other category", StatusCodes.Status500InternalServerError);

        return otherCategory.Id;
    }
}
