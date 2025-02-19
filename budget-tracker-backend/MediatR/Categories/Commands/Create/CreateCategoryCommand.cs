using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Categories;

namespace budget_tracker_backend.MediatR.Categories.Commands.Create;

public record CreateCategoryCommand(CreateCategoryDto NewCategory) : IRequest<Result<CategoryDto>>;