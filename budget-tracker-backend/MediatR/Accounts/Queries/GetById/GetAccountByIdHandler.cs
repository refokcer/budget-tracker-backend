using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Exceptions;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Accounts.Queries.GetById;

public class GetAccountByIdHandler : IRequestHandler<GetAccountByIdQuery, Result<AccountDto>>
{
    private readonly IAccountManager _manager;
    private readonly IMapper _mapper;

    public GetAccountByIdHandler(IAccountManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (account == null)
        {
            const string errorMsg = "Account not found";
            throw new CustomException(errorMsg, StatusCodes.Status404NotFound);
        }

        var dto = _mapper.Map<AccountDto>(account);
        return Result.Ok(dto);
    }
}
