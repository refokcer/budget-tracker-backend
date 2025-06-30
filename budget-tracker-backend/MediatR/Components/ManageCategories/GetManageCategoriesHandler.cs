using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Services.Components;

namespace budget_tracker_backend.MediatR.Components.ManageCategories;

public class GetManageCategoriesHandler : IRequestHandler<GetManageCategoriesQuery, Result<ManageCategoriesDto>>
{
    private readonly IComponentManager _manager;
    private readonly IMapper _mapper;

    public GetManageCategoriesHandler(IComponentManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<ManageCategoriesDto>> Handle(GetManageCategoriesQuery request, CancellationToken cancellationToken)
    {
        var dto = await _manager.GetManageCategoriesAsync(request.Type, cancellationToken);
        return Result.Ok(dto);
    }
}

