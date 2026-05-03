using AutoMapper;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Queries.GetById;

public class GetFinancialGoalByIdHandler : IRequestHandler<GetFinancialGoalByIdQuery, Result<FinancialGoalDto>>
{
    private readonly IFinancialGoalManager _manager;
    private readonly IMapper _mapper;

    public GetFinancialGoalByIdHandler(IFinancialGoalManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<FinancialGoalDto>> Handle(GetFinancialGoalByIdQuery request, CancellationToken cancellationToken)
    {
        var goal = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (goal == null)
            throw new CustomException("Financial goal not found", StatusCodes.Status404NotFound);

        return Result.Ok(_mapper.Map<FinancialGoalDto>(goal));
    }
}
