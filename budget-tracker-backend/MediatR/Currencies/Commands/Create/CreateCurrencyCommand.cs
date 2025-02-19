using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Commands.Create;

public record CreateCurrencyCommand(CreateCurrencyDto NewCurrency) : IRequest<Result<CurrencyDto>>;