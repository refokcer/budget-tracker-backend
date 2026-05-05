using budget_tracker_backend.Dto.FinancialGoals;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.FinancialGoals.Commands.ApplyBudgetAdjustments;

public record ApplyBudgetAdjustmentsCommand(
    int GoalId,
    ApplyBudgetAdjustmentsRequestDto? Dto) : IRequest<Result<ApplyBudgetAdjustmentsResultDto>>;
