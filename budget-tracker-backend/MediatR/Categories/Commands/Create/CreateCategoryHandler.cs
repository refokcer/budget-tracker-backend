using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Commands.Create;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateCategoryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Category>(request.NewCategory);
        if (entity == null)
        {
            return Result.Fail("Failed to map CreateCategoryDto to Category");
        }

        _context.Categories.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to create Category in database");
        }

        // Мапим обратно в CategoryDto
        var resultDto = _mapper.Map<CategoryDto>(entity);
        return Result.Ok(resultDto);
    }
}
