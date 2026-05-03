using budget_tracker_backend.Dto.UserSettings;
using budget_tracker_backend.Services.UserSettings;
using FluentResults;
using MediatR;

namespace budget_tracker_backend.MediatR.UserSettings.Queries;

public class GetUserSettingsHandler : IRequestHandler<GetUserSettingsQuery, Result<UserSettingsDto>>
{
    private readonly IUserSettingsManager _manager;

    public GetUserSettingsHandler(IUserSettingsManager manager)
    {
        _manager = manager;
    }

    public Task<Result<UserSettingsDto>> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        return _manager.GetAsync(cancellationToken);
    }
}
