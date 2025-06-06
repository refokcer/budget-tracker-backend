﻿using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Helpers;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Update;

public class UpdateTransactionHandler
    : IRequestHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public UpdateTransactionHandler(ApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        var dto = request.TransactionToUpdate;
        var tr = await _ctx.Transactions.FirstOrDefaultAsync(t => t.Id == dto.Id, ct);
        if (tr is null)
            return Result.Fail($"Transaction {dto.Id} not found");

        // --- 1) откатываем старые значения ----------------------------------
        var oldFrom = tr.AccountFrom.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountFrom.Value }, ct)
            : null;

        var oldTo = tr.AccountTo.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountTo.Value }, ct)
            : null;

        AccountBalanceHelper.Apply(tr.Type, tr.Amount, oldFrom, oldTo, reverse: true);

        // --- 2) применяем новые значения ------------------------------------
        _mapper.Map(dto, tr);                // копируем поля из DTO → entity

        // проверяем / загружаем новые аккаунты
        var newFrom = tr.AccountFrom.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountFrom.Value }, ct)
            : null;

        var newTo = tr.AccountTo.HasValue
            ? await _ctx.Accounts.FindAsync(new object[] { tr.AccountTo.Value }, ct)
            : null;

        // если передан несуществующий счёт — вернём ошибку
        if (tr.AccountFrom.HasValue && newFrom == null)
            return Result.Fail("AccountFrom not found");
        if (tr.AccountTo.HasValue && newTo == null)
            return Result.Fail("AccountTo not found");

        AccountBalanceHelper.Apply(tr.Type, tr.Amount, newFrom, newTo, reverse: false);

        var saved = await _ctx.SaveChangesAsync(ct) > 0;
        if (!saved)
            return Result.Fail("Failed to update Transaction");

        return Result.Ok(_mapper.Map<TransactionDto>(tr));
    }
}
