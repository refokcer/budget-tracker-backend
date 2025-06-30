using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Queries.GetById;

public class GetCurrencyByIdHandler : IRequestHandler<GetCurrencyByIdQuery, Result<CurrencyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCurrencyByIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CurrencyDto>> Handle(GetCurrencyByIdQuery request, CancellationToken cancellationToken)
    {
        var currency = await _context.Currencies.FindAsync(new object[] { request.Id }, cancellationToken);
        if (currency == null)
        {
            return Result.Fail($"Currency with Id={request.Id} not found");
        }

        var dto = _mapper.Map<CurrencyDto>(currency);
        return Result.Ok(dto);
    }
}