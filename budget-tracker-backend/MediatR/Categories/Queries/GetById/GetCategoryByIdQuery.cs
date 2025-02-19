using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Categories;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetById;

public record GetCategoryByIdQuery(int Id) : IRequest<Result<CategoryDto>>;