using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlans;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.BudgetPlans.Queries.GetById;

public class GetBudgetPlanByIdHandler
    : IRequestHandler<GetBudgetPlanByIdQuery, Result<BudgetPlanDto>>
{
    private readonly IBudgetPlanManager _manager;
    private readonly IMapper _mapper;

    public GetBudgetPlanByIdHandler(IBudgetPlanManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanDto>> Handle(GetBudgetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (plan == null)
            return Result.Fail($"BudgetPlan with Id={request.Id} not found");

        var dto = _mapper.Map<BudgetPlanDto>(plan);
        return Result.Ok(dto);
    }
}