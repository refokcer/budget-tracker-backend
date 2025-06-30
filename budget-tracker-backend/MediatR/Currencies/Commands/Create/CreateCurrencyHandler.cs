using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Create;

public class CreateCurrencyHandler
    : IRequestHandler<CreateCurrencyCommand, Result<CurrencyDto>>
{
    private readonly ICurrencyManager _manager;
    private readonly IMapper _mapper;

    public CreateCurrencyHandler(ICurrencyManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.CreateAsync(request.NewCurrency, cancellationToken);
        var currencyDto = _mapper.Map<CurrencyDto>(entity);
        return Result.Ok(currencyDto);
    }
}