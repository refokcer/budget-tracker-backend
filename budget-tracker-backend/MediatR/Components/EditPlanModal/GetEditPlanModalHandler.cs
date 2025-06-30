using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Services.Components;

namespace budget_tracker_backend.MediatR.Components.EditPlanModal;

public class GetEditPlanModalHandler : IRequestHandler<GetEditPlanModalQuery, Result<EditPlanModalDto>>
{
    private readonly IComponentManager _manager;
    private readonly IMapper _mapper;

    public GetEditPlanModalHandler(IComponentManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<EditPlanModalDto>> Handle(GetEditPlanModalQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetEditPlanModalAsync(cancellationToken);
        return Result.Ok(dto);
    }
}

