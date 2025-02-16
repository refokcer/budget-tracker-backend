using budget_tracker_backend.Dto.Accounts;
using FluentResults;
using MediatR;
using System;

namespace budget_tracker_backend.MediatR.Accounts.Queries.GetAll;

public record GetAllAccountsQuery : IRequest<Result<IEnumerable<AccountDto>>>;