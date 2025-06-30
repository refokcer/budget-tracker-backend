using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Dto.Categories;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetAll;

public class GetAllCategoriesHandler
    : IRequestHandler<GetAllCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    private readonly ICategoryManager _manager;
    private readonly IMapper _mapper;

    public GetAllCategoriesHandler(ICategoryManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _manager.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
        return Result.Ok(dtos);
    }
}