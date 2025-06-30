using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetByType;

public class GetCategoriesByTypeHandler
    : IRequestHandler<GetCategoriesByTypeQuery, Result<IEnumerable<CategoryDto>>>
{
    private readonly ICategoryManager _manager;
    private readonly IMapper _mapper;

    public GetCategoriesByTypeHandler(ICategoryManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesByTypeQuery request, CancellationToken cancellationToken)
    {
        // Filter categories by specified Type
        var categories = await _manager.GetByTypeAsync(request.Type, cancellationToken);

        // Mapim in DTO
        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
        return Result.Ok(dtos);
    }
}
