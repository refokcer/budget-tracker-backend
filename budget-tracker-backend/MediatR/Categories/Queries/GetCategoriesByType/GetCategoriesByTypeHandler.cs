using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetByType;

public class GetCategoriesByTypeHandler
    : IRequestHandler<GetCategoriesByTypeQuery, Result<IEnumerable<CategoryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoriesByTypeHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesByTypeQuery request, CancellationToken cancellationToken)
    {
        // Filter categories by specified Type
        var categories = await _context.Categories
            .Where(c => c.Type == request.Type)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Mapim in DTO
        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
        return Result.Ok(dtos);
    }
}
