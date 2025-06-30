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


        if (type == TransactionCategoryType.Income)
        {
            if (entity.AccountTo.HasValue)
            {
                var account = await _accountManager.GetByIdAsync(entity.AccountTo.Value, token);
                if (account == null)
                    return Result.Fail("AccountTo not found");

                if (entity.Amount <= 0)
                    return Result.Fail("Income must be >0");

                await _accountManager.ApplyBalanceAsync(type, entity.Amount, null, account, false, token);
            }
        }
        else if (type == TransactionCategoryType.Expense)
        {
            if (entity.AccountFrom.HasValue)
            {
                var account = await _accountManager.GetByIdAsync(entity.AccountFrom.Value, token);
                if (account == null)
                    return Result.Fail("AccountFrom not found");

                if (account.Amount - entity.Amount < 0)
                    return Result.Fail("Not enough money");

                await _accountManager.ApplyBalanceAsync(type, entity.Amount, account, null, false, token);
            }
        }
        else if (type == TransactionCategoryType.Transaction)
        {
            Account? fromAcc = null;
            if (entity.AccountFrom.HasValue)
            {
                fromAcc = await _accountManager.GetByIdAsync(entity.AccountFrom.Value, token);
                if (fromAcc == null)
                    return Result.Fail("AccountFrom not found");

                if(fromAcc.Amount - entity.Amount < 0)
                    return Result.Fail("Not enough money");
            }

            Account? toAcc = null;
            if (entity.AccountTo.HasValue)
            {
                toAcc = await _accountManager.GetByIdAsync(entity.AccountTo.Value, token);
                if (toAcc == null)
                    return Result.Fail("AccountTo not found");
            }

            await _accountManager.ApplyBalanceAsync(type, entity.Amount, fromAcc, toAcc, false, token);
        }
        else if (type == TransactionCategoryType.None)
        {
            return Result.Fail("Transaction typy not defined");
        }

        _context.Transactions.Add(entity);
        var saved = await _context.SaveChangesAsync(token) > 0;

        if (!saved)
            return Result.Fail("Failed to create transaction");

        var resultDto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(resultDto);
    }
}
