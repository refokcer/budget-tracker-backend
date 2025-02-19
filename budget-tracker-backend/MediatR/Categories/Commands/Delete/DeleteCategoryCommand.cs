using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.Categories.Commands.Delete;

public record DeleteCategoryCommand(int Id) : IRequest<Result<bool>>;