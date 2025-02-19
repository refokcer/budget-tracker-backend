using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Delete;

public class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteTransactionHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Transactions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"Transaction with Id={request.Id} not found");
        }

        _context.Transactions.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        return saved
            ? Result.Ok(true)
            : Result.Fail("Failed to delete Transaction");
    }
}