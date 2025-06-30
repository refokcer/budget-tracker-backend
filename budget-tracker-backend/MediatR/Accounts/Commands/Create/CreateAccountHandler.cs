using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Services.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Create;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
{
    private readonly IAccountManager _manager;
    private readonly IMapper _mapper;

    public CreateAccountHandler(IAccountManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _manager.CreateAsync(request.NewAccount, cancellationToken);
        var dto = _mapper.Map<AccountDto>(account);
        return Result.Ok(dto);
    }
}
