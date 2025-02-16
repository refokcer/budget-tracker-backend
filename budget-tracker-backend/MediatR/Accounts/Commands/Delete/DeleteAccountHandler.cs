using budget_tracker_backend.Data;
using budget_tracker_backend.Exceptions;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Delete;

public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Result<bool>>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteAccountHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { request.Id }, cancellationToken);
        if (account == null)
        {
            const string errorMsg = "Account not found";
            throw new CustomException(errorMsg, StatusCodes.Status404NotFound);
        }

        _dbContext.Accounts.Remove(account);
        var saveResult = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        if (!saveResult)
        {
            const string errorMsg = "Failed to delete account";
            throw new CustomException(errorMsg, StatusCodes.Status500InternalServerError);
        }

        return Result.Ok(true);
    }
}
