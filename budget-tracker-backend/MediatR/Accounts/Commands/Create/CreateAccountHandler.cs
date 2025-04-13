using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Create;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateAccountHandler(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Mapping DTO → model
        var entity = _mapper.Map<Account>(request.NewAccount);
        if (entity == null)
        {
            const string errorMsg = "Cannot map CreateAccountDto to Account entity";
            throw new CustomException(errorMsg, StatusCodes.Status400BadRequest);
        }

        // Add record to DbContext
        await _dbContext.Accounts.AddAsync(entity, cancellationToken);
        var saveResult = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

        if (!saveResult)
        {
            const string errorMsg = "Failed to create an account";
            throw new CustomException(errorMsg, StatusCodes.Status500InternalServerError);
        }

        // Returning a successful result
        var resultDto = _mapper.Map<AccountDto>(entity);
        return Result.Ok(resultDto);
    }
}
