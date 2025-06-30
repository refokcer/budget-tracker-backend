using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Services.Components;

namespace budget_tracker_backend.MediatR.Components.IncomeModal;

public class GetIncomeModalHandler : IRequestHandler<GetIncomeModalQuery, Result<IncomeModalDto>>
{
    private readonly IComponentManager _manager;
    private readonly IMapper _mapper;

    public GetIncomeModalHandler(IComponentManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IncomeModalDto>> Handle(GetIncomeModalQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetIncomeModalAsync(cancellationToken);
        return Result.Ok(dto);
    }
}
