using AutoMapper;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Services.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.Create;

public class CreateFinancialGoalHandler : IRequestHandler<CreateFinancialGoalCommand, Result<FinancialGoalDto>>
{
    private readonly IFinancialGoalManager _manager;
    private readonly IMapper _mapper;

    public CreateFinancialGoalHandler(IFinancialGoalManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<FinancialGoalDto>> Handle(CreateFinancialGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _manager.CreateAsync(request.Goal, cancellationToken);
        return Result.Ok(_mapper.Map<FinancialGoalDto>(goal));
    }
}
