using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Currencies;

namespace budget_tracker_backend.MediatR.Currencies.Queries.GetAll;

public record GetAllCurrenciesQuery : IRequest<Result<IEnumerable<CurrencyDto>>>;