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

        // Если Type не задан в dto, можно сказать "Ошибка" или "Type=Transaction" по умолчанию
        var type = dto.Type ?? TransactionCategoryType.Transaction;

        // Мапим DTO -> модель
        var entity = _mapper.Map<Transaction>(dto);

        // Принудительно устанавливаем Type
        entity.Type = type;

        // Логика обновления счётов:
        // 1. Если это Income => AccountTo != null => accountTo.Amount += dto.Amount
        // 2. Если Expense => AccountFrom != null => accountFrom.Amount -= dto.Amount
        // 3. Если Transfer => AccountFrom != null => accountFrom.Amount -= dto.Amount
        //                  AccountTo != null => accountTo.Amount += dto.Amount

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
            // "Перевод" (между двумя счетами)
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

        // Добавляем транзакцию
        _context.Transactions.Add(entity);
        var saved = await _context.SaveChangesAsync(token) > 0;

        if (!saved)
            return Result.Fail("Failed to create transaction");

        var resultDto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(resultDto);
    }
}