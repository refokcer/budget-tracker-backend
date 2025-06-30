using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Commands.Update;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var dto = request.CategoryToUpdate;

        var existing = await _context.Categories.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            return Result.Fail($"Category with Id={dto.Id} not found");
        }

        _mapper.Map(dto, existing);

        _context.Categories.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update Category");
        }

        var resultDto = _mapper.Map<CategoryDto>(existing);
        return Result.Ok(resultDto);
    }
}
