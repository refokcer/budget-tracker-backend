using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Accounts;

namespace budget_tracker_backend.MediatR.Components.ManageAccounts;

public class GetManageAccountsHandler : IRequestHandler<GetManageAccountsQuery, Result<ManageAccountsDto>>
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetManageAccountsHandler(ApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<ManageAccountsDto>> Handle(GetManageAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _ctx.Accounts.AsNoTracking().ToListAsync(cancellationToken);

        var dto = new ManageAccountsDto
        {
            Accounts = _mapper.Map<List<AccountDto>>(accounts)
        };

        return Result.Ok(dto);
    }
}

