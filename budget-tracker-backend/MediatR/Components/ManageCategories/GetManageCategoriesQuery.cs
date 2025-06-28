using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Components.ManageCategories;

public record GetManageCategoriesQuery(TransactionCategoryType Type) : IRequest<Result<ManageCategoriesDto>>;

