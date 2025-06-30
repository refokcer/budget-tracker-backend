using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Components.TransferModal;

public class GetTransferModalHandler : IRequestHandler<GetTransferModalQuery, Result<TransferModalDto>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetTransferModalHandler(IApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<TransferModalDto>> Handle(GetTransferModalQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _ctx.Currencies.AsNoTracking().ToListAsync(cancellationToken);
        var accounts = await _ctx.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await _ctx.Categories
            .Where(c => c.Type == TransactionCategoryType.Transaction)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dto = new TransferModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Accounts = _mapper.Map<List<AccountDto>>(accounts),
            Categories = _mapper.Map<List<CategoryDto>>(categories)
        };

        return Result.Ok(dto);
    }
}
