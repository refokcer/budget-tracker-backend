using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Queries.GetById;

public record GetCurrencyByIdQuery(int Id) : IRequest<Result<CurrencyDto>>;