using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Update;

public class UpdateCurrencyHandler : IRequestHandler<UpdateCurrencyCommand, Result<CurrencyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateCurrencyHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.CurrencyToUpdate;
        var existing = await _context.Currencies.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            return Result.Fail($"Currency with Id={dto.Id} not found");
        }

        _mapper.Map(dto, existing);

        _context.Currencies.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update Currency in database");
        }

        var updatedDto = _mapper.Map<CurrencyDto>(existing);
        return Result.Ok(updatedDto);
    }
}