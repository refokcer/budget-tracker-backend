using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Delete;

public class DeleteCurrencyHandler : IRequestHandler<DeleteCurrencyCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteCurrencyHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Currencies.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"Currency with Id={request.Id} not found");
        }

        _context.Currencies.Remove(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        return saved
            ? Result.Ok(true)
            : Result.Fail("Failed to delete Currency");
    }
}