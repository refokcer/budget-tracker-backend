using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.Dto.UserSettings;
using budget_tracker_backend.MediatR.UserSettings.Commands;
using budget_tracker_backend.MediatR.UserSettings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserSettingsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await Mediator.Send(new GetUserSettingsQuery());
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UserSettingsDto dto)
    {
        var result = await Mediator.Send(new UpdateUserSettingsCommand(dto));
        return HandleResult(result);
    }
}
