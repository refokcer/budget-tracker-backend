namespace budget_tracker_backend.Services.UserSettings;

using budget_tracker_backend.Dto.UserSettings;
using FluentResults;

public interface IUserSettingsManager
{
    Task<Result<UserSettingsDto>> GetAsync(CancellationToken cancellationToken);
    Task<Result<UserSettingsDto>> UpdateAsync(UserSettingsDto dto, CancellationToken cancellationToken);
}
