using budget_tracker_backend.Dto.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Create;

public record CreateAccountCommand(CreateAccountDto NewAccount) : IRequest<Result<AccountDto>>;

