using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Components;
using budget_tracker_backend.Dto.Components;

namespace budget_tracker_backend.MediatR.Components.ManageAccounts;

public class GetManageAccountsHandler : IRequestHandler<GetManageAccountsQuery, Result<ManageAccountsDto>>
{
    private readonly IMapper _mapper;
    private readonly IComponentManager _manager;

    public GetManageAccountsHandler(IMapper mapper, IComponentManager manager)
    {
        _mapper = mapper;
        _manager = manager;
    }

    public async Task<Result<ManageAccountsDto>> Handle(GetManageAccountsQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetManageAccountsAsync(cancellationToken);
        return Result.Ok(dto);
    }
}

