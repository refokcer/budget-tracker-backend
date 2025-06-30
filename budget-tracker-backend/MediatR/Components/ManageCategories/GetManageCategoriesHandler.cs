using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Components.ManageCategories;

public class GetManageCategoriesHandler : IRequestHandler<GetManageCategoriesQuery, Result<ManageCategoriesDto>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetManageCategoriesHandler(IApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<ManageCategoriesDto>> Handle(GetManageCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _ctx.Categories
            .Where(c => c.Type == request.Type)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dto = new ManageCategoriesDto
        {
            Categories = _mapper.Map<List<CategoryDto>>(categories)
        };

        return Result.Ok(dto);
    }
}

