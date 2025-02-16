using budget_tracker_backend.Data;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Commands.Delete;

public class DeleteBudgetPlanHandler : IRequestHandler<DeleteBudgetPlanCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteBudgetPlanHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteBudgetPlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.BudgetPlans.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"BudgetPlan with Id={request.Id} not found");
        }

        _context.BudgetPlans.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        return saved
            ? Result.Ok(true)
            : Result.Fail("Failed to delete BudgetPlan");
    }
}