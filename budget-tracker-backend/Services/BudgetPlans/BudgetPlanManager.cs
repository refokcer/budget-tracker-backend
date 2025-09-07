namespace budget_tracker_backend.Services.BudgetPlans;

using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class BudgetPlanManager : IBudgetPlanManager
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BudgetPlanManager(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BudgetPlan>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.BudgetPlans
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetPlan?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.BudgetPlans.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<BudgetPlan> CreateAsync(CreateBudgetPlanDto dto, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<BudgetPlan>(dto) ??
            throw new CustomException("Cannot map CreateBudgetPlanDto", StatusCodes.Status400BadRequest);

        entity.ParentId = await ValidateParentAsync(entity.Type, dto.ParentId, cancellationToken);

        await _context.BudgetPlans.AddAsync(entity, cancellationToken);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to create budget plan", StatusCodes.Status500InternalServerError);

        return entity;
    }

    public async Task<BudgetPlan> UpdateAsync(BudgetPlanDto dto, CancellationToken cancellationToken)
    {
        var existing = await _context.BudgetPlans.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
            throw new CustomException("Budget plan not found", StatusCodes.Status404NotFound);

        existing.Title = dto.Title;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.Type = dto.Type;
        existing.Description = dto.Description;
        existing.ParentId = await ValidateParentAsync(dto.Type, dto.ParentId, cancellationToken);

        _context.BudgetPlans.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to update budget plan", StatusCodes.Status500InternalServerError);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var plan = await _context.BudgetPlans.FindAsync(new object[] { id }, cancellationToken);
        if (plan == null)
            throw new CustomException("Budget plan not found", StatusCodes.Status404NotFound);

        _context.BudgetPlans.Remove(plan);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
            throw new CustomException("Failed to delete budget plan", StatusCodes.Status500InternalServerError);

        return true;
    }

    private async Task<int?> ValidateParentAsync(BudgetPlanType type, int? parentId, CancellationToken cancellationToken)
    {
        if (type == BudgetPlanType.Monthly)
        {
            if (parentId.HasValue)
                throw new CustomException("Monthly plan cannot have parent", StatusCodes.Status400BadRequest);
            return null;
        }

        if (!parentId.HasValue)
            return null;

        var parent = await _context.BudgetPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == parentId.Value, cancellationToken);
        if (parent == null || parent.Type != BudgetPlanType.Monthly)
            throw new CustomException("Invalid parent plan", StatusCodes.Status400BadRequest);

        return parentId;
    }

}
