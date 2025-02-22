using FluentResults;
using MediatR;
using budget_tracker_backend.Data;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Delete;

public class DeleteBudgetPlanItemHandler : IRequestHandler<DeleteBudgetPlanItemCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteBudgetPlanItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.BudgetPlanItems.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"BudgetPlanItem with Id={request.Id} not found");
        }

        _context.BudgetPlanItems.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        return saved ? Result.Ok(true) : Result.Fail("Failed to delete BudgetPlanItem");
    }
}