using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using FluentResults;
using MediatR;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateTransactionHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
                var account = await _context.Accounts.FindAsync(entity.AccountTo.Value);
                if (account == null)
                    return Result.Fail("AccountTo not found");

                account.Amount += entity.Amount;
            }
        }
        else if (type == TransactionCategoryType.Expense)
        {
            if (entity.AccountFrom.HasValue)
            {
                var account = await _context.Accounts.FindAsync(entity.AccountFrom.Value);
                if (account == null)
                    return Result.Fail("AccountFrom not found");

                account.Amount -= entity.Amount;
            }
        }
        else if (type == TransactionCategoryType.Transaction)
        {
            if (entity.AccountFrom.HasValue)
            {
                var fromAcc = await _context.Accounts.FindAsync(entity.AccountFrom.Value);
                if (fromAcc == null)
                    return Result.Fail("AccountFrom not found");
                fromAcc.Amount -= entity.Amount;
            }
            if (entity.AccountTo.HasValue)
            {
                var toAcc = await _context.Accounts.FindAsync(entity.AccountTo.Value);
                if (toAcc == null)
                    return Result.Fail("AccountTo not found");
                toAcc.Amount += entity.Amount;
            }
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