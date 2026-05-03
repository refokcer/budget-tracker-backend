using AutoMapper;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetAll;

public class GetAllFinancialGoalsHandler : IRequestHandler<GetAllFinancialGoalsQuery, Result<IEnumerable<FinancialGoalDto>>>
{
    private readonly IFinancialGoalManager _manager;
    private readonly IMapper _mapper;

    public GetAllFinancialGoalsHandler(IFinancialGoalManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<FinancialGoalDto>>> Handle(GetAllFinancialGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await _manager.GetAllAsync(cancellationToken);
        return Result.Ok(_mapper.Map<IEnumerable<FinancialGoalDto>>(goals));
    }
}
