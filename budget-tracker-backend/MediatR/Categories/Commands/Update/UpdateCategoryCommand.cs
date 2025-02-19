using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Categories;

namespace budget_tracker_backend.MediatR.Categories.Commands.Update;

public record UpdateCategoryCommand(CategoryDto CategoryToUpdate) : IRequest<Result<CategoryDto>>;