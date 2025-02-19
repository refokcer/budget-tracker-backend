using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Categories;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetAll;

public record GetAllCategoriesQuery : IRequest<Result<IEnumerable<CategoryDto>>>;