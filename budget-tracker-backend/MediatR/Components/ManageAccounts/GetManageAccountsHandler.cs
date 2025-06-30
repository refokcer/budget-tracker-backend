using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Accounts;

namespace budget_tracker_backend.MediatR.Components.ManageAccounts;

public class GetManageAccountsHandler : IRequestHandler<GetManageAccountsQuery, Result<ManageAccountsDto>>
{
    private readonly IMapper _mapper;
    private readonly IAccountManager _accountManager;

    public GetManageAccountsHandler(IMapper mapper, IAccountManager accountManager)
    {
        _mapper = mapper;
        _accountManager = accountManager;
    }

    public async Task<Result<ManageAccountsDto>> Handle(GetManageAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _accountManager.GetAllAsync(cancellationToken);

        var dto = new ManageAccountsDto
        {
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };

        return Result.Ok(dto);
    }
}

