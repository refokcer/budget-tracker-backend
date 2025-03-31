using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Categories.Queries.GetByType;

public record GetCategoriesByTypeQuery(TransactionCategoryType Type)
    : IRequest<Result<IEnumerable<CategoryDto>>>;
