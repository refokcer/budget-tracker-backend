using budget_tracker_backend.Dto.UserSettings;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.UserSettings.Commands;

public record UpdateUserSettingsCommand(UserSettingsDto Dto) : IRequest<Result<UserSettingsDto>>;
