using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;

namespace budget_tracker_backend.MediatR.Components.ManageAccounts;

public record GetManageAccountsQuery : IRequest<Result<ManageAccountsDto>>;

