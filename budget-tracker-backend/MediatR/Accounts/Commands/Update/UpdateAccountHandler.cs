using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Services.Accounts;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Commands.Update;

public class UpdateAccountHandler : IRequestHandler<UpdateAccountCommand, Result<AccountDto>>
{
    private readonly IAccountManager _manager;
    private readonly IMapper _mapper;

    public UpdateAccountHandler(IAccountManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _manager.UpdateAsync(request.UpdatedAccount, cancellationToken);
        var dto = _mapper.Map<AccountDto>(account);
        return Result.Ok(dto);
    }
}