using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Components.EditPlanModal;

public class GetEditPlanModalHandler : IRequestHandler<GetEditPlanModalQuery, Result<EditPlanModalDto>>
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetEditPlanModalHandler(ApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<EditPlanModalDto>> Handle(GetEditPlanModalQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _ctx.Currencies.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await _ctx.Categories
            .Where(c => c.Type == TransactionCategoryType.Expense)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dto = new EditPlanModalDto
        {
            Categories = _mapper.Map<List<CategoryDto>>(categories),
            Currencies = _mapper.Map<List<CurrencyDto>>(currencies)
        };

        return Result.Ok(dto);
    }
}

