using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Create;

public class CreateCurrencyHandler
    : IRequestHandler<CreateCurrencyCommand, Result<CurrencyDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateCurrencyHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Currency>(request.NewCurrency);
        if (entity == null)
        {
            return Result.Fail("Cannot map CreateCurrencyDto to Currency entity");
        }

        _context.Currencies.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to create Currency in database");
        }

        var currencyDto = _mapper.Map<CurrencyDto>(entity);
        return Result.Ok(currencyDto);
    }
}