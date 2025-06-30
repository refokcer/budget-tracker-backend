using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Queries.GetAll;

public class GetAllCurrenciesHandler : IRequestHandler<GetAllCurrenciesQuery, Result<IEnumerable<CurrencyDto>>>
{
    private readonly ICurrencyManager _manager;
    private readonly IMapper _mapper;

    public GetAllCurrenciesHandler(ICurrencyManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CurrencyDto>>> Handle(GetAllCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<CurrencyDto>>(currencies);
        return Result.Ok(dtos);
    }
}