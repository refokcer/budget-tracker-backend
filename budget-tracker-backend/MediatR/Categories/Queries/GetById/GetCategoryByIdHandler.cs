using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetById;

public class GetCategoryByIdHandler
    : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null)
        {
            return Result.Fail($"Category with Id={request.Id} not found");
        }

        var dto = _mapper.Map<CategoryDto>(category);
        return Result.Ok(dto);
    }
}