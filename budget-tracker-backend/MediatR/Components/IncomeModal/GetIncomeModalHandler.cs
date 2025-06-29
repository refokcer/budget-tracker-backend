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

namespace budget_tracker_backend.MediatR.Components.IncomeModal;

public class GetIncomeModalHandler : IRequestHandler<GetIncomeModalQuery, Result<IncomeModalDto>>
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetIncomeModalHandler(ApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<IncomeModalDto>> Handle(GetIncomeModalQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _ctx.Currencies.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await _ctx.Categories
            .Where(c => c.Type == TransactionCategoryType.Income)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var accounts = await _ctx.Accounts.AsNoTracking().ToListAsync(cancellationToken);

        var dto = new IncomeModalDto
        {
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies),
            Categories = _mapper.Map<List<CategoryDto>>(categories),
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };

        return Result.Ok(dto);
    }
}
