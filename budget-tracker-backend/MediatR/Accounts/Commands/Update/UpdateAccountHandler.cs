using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Exceptions;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Update;

public class UpdateAccountHandler : IRequestHandler<UpdateAccountCommand, Result<AccountDto>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateAccountHandler(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        // Pull from the database
        var existing = await _dbContext.Accounts.FindAsync(new object[] { request.UpdatedAccount.Id }, cancellationToken);
        if (existing == null)
        {
            const string errorMsg = "Account not found";
            throw new CustomException(errorMsg, StatusCodes.Status404NotFound);
        }

        // Transfer data from DTO to entity
        existing.Title = request.UpdatedAccount.Title;
        existing.Amount = request.UpdatedAccount.Amount;
        existing.CurrencyId = request.UpdatedAccount.CurrencyId;
        existing.Description = request.UpdatedAccount.Description;

        _dbContext.Accounts.Update(existing);
        var saveResult = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        if (!saveResult)
        {
            const string errorMsg = "Failed to update account";
            throw new CustomException(errorMsg, StatusCodes.Status500InternalServerError);
        }

        var resultDto = _mapper.Map<AccountDto>(existing);
        return Result.Ok(resultDto);
    }
}