using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetById;

public class GetCategoryByIdHandler
    : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly ICategoryManager _manager;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(ICategoryManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _manager.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            return Result.Fail($"Category with Id={request.Id} not found");

        var dto = _mapper.Map<CategoryDto>(category);
        return Result.Ok(dto);
    }
}