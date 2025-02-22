using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetById;

public class GetBudgetPlanItemByIdHandler : IRequestHandler<GetBudgetPlanItemByIdQuery, Result<BudgetPlanItemDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetBudgetPlanItemByIdHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(GetBudgetPlanItemByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.BudgetPlanItems.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null)
        {
            return Result.Fail($"BudgetPlanItem with Id={request.Id} not found");
        }

        var dto = _mapper.Map<BudgetPlanItemDto>(entity);
        return Result.Ok(dto);
    }
}