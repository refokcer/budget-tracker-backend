using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Commands.Update;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly ICategoryManager _manager;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(ICategoryManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _manager.UpdateAsync(request.CategoryToUpdate, cancellationToken);
        var resultDto = _mapper.Map<CategoryDto>(entity);
        return Result.Ok(resultDto);
    }
}
