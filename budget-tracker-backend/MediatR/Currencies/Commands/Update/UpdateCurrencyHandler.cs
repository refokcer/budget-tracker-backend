using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Update;

public class UpdateCurrencyHandler : IRequestHandler<UpdateCurrencyCommand, Result<CurrencyDto>>
{
    private readonly ICurrencyManager _manager;
    private readonly IMapper _mapper;

    public UpdateCurrencyHandler(ICurrencyManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.UpdateAsync(request.CurrencyToUpdate, cancellationToken);
        var updatedDto = _mapper.Map<CurrencyDto>(entity);
        return Result.Ok(updatedDto);
    }
}