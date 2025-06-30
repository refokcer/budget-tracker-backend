using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Services.Components;

namespace budget_tracker_backend.MediatR.Components.TransferModal;

public class GetTransferModalHandler : IRequestHandler<GetTransferModalQuery, Result<TransferModalDto>>
{
    private readonly IComponentManager _manager;
    private readonly IMapper _mapper;

    public GetTransferModalHandler(IComponentManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<TransferModalDto>> Handle(GetTransferModalQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetTransferModalAsync(cancellationToken);
        return Result.Ok(dto);
    }
}
