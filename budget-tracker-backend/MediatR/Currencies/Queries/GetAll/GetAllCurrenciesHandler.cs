using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Queries.GetAll;

public class GetAllCurrenciesHandler : IRequestHandler<GetAllCurrenciesQuery, Result<IEnumerable<CurrencyDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllCurrenciesHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CurrencyDto>>> Handle(GetAllCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _context.Currencies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<CurrencyDto>>(currencies);
        return Result.Ok(dtos);
    }
}