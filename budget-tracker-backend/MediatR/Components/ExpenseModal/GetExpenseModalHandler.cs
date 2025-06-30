using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Services.Components;

namespace budget_tracker_backend.MediatR.Components.ExpenseModal;

public class GetExpenseModalHandler : IRequestHandler<GetExpenseModalQuery, Result<ExpenseModalDto>>
{
    private readonly IComponentManager _manager;
    private readonly IMapper _mapper;

    public GetExpenseModalHandler(IComponentManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<ExpenseModalDto>> Handle(GetExpenseModalQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetExpenseModalAsync(cancellationToken);
        return Result.Ok(dto);
    }
}
