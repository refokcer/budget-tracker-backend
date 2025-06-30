using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Commands.Create;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly ICategoryManager _manager;
    private readonly IMapper _mapper;

    public CreateCategoryHandler(ICategoryManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.CreateAsync(request.NewCategory, cancellationToken);
        var resultDto = _mapper.Map<CategoryDto>(entity);
        return Result.Ok(resultDto);
    }
}
