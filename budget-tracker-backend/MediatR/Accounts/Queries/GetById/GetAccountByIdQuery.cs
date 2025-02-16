using budget_tracker_backend.Dto.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Queries.GetById;

public record GetAccountByIdQuery(int Id) : IRequest<Result<AccountDto>>;