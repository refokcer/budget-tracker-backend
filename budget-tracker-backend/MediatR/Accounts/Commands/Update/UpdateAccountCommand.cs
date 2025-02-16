using budget_tracker_backend.Dto.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Update;

public record UpdateAccountCommand(AccountDto UpdatedAccount) : IRequest<Result<AccountDto>>;
