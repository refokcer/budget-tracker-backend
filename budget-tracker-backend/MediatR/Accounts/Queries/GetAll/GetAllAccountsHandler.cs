using AutoMapper;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Accounts;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace budget_tracker_backend.MediatR.Accounts.Queries.GetAll;

public class GetAllAccountsHandler : IRequestHandler<GetAllAccountsQuery, Result<IEnumerable<AccountDto>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllAccountsHandler(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<AccountDto>>> Handle(GetAllAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Мапим список Account → список AccountDto
        var dtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);
        return Result.Ok(dtos);
    }
}