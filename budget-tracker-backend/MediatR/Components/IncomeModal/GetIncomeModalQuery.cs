using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Components;

namespace budget_tracker_backend.MediatR.Components.IncomeModal;

public record GetIncomeModalQuery : IRequest<Result<IncomeModalDto>>;
