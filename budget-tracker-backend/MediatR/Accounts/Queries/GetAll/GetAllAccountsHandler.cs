using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Services.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Queries.GetAll;

public class GetAllAccountsHandler : IRequestHandler<GetAllAccountsQuery, Result<IEnumerable<AccountDto>>>
{
    private readonly IAccountManager _manager;
    private readonly IMapper _mapper;

    public GetAllAccountsHandler(IAccountManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<AccountDto>>> Handle(GetAllAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _manager.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);
        return Result.Ok(dtos);
    }
}