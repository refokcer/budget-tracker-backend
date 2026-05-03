using budget_tracker_backend.Dto.UserSettings;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.UserSettings.Queries;

public record GetUserSettingsQuery : IRequest<Result<UserSettingsDto>>;
