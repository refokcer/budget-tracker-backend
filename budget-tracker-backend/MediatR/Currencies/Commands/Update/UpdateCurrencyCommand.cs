using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Update;

public record UpdateCurrencyCommand(CurrencyDto CurrencyToUpdate) : IRequest<Result<CurrencyDto>>;