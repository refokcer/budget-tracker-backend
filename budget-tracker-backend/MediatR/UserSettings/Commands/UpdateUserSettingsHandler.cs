using budget_tracker_backend.Dto.UserSettings;
using budget_tracker_backend.Services.UserSettings;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.UserSettings.Commands;

public class UpdateUserSettingsHandler : IRequestHandler<UpdateUserSettingsCommand, Result<UserSettingsDto>>
{
    private readonly IUserSettingsManager _manager;

    public UpdateUserSettingsHandler(IUserSettingsManager manager)
    {
        _manager = manager;
    }

    public Task<Result<UserSettingsDto>> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        return _manager.UpdateAsync(request.Dto, cancellationToken);
    }
}
