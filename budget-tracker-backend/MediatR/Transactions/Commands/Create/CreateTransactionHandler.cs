using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using FluentResults;
using MediatR;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAccountManager _accountManager;

    public CreateTransactionHandler(IApplicationDbContext context, IMapper mapper, IAccountManager accountManager)
    {
        _context = context;
        _mapper = mapper;
        _accountManager = accountManager;
    }

    public async Task<Result<TransactionDto>> Handle(CreateTransactionCommand request, CancellationToken token)
    {
        var dto = request.NewTransaction;

        // "Type=Transaction" by default
        var type = dto.Type ?? TransactionCategoryType.None;

        var entity = _mapper.Map<Transaction>(dto);

        entity.Type = type;


        if (type == TransactionCategoryType.None)
        {
            return Result.Fail("Transaction typy not defined");
        }

        var result = await _accountManager.HandleTransactionAsync(
            type,
            entity.Amount,
            entity.AccountFrom,
            entity.AccountTo,
            false,
            token);

        if (result.IsFailed)
            return Result.Fail(result.Errors.First().Message);

        _context.Transactions.Add(entity);
        var saved = await _context.SaveChangesAsync(token) > 0;

        if (!saved)
            return Result.Fail("Failed to create transaction");

        var resultDto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(resultDto);
    }
}
