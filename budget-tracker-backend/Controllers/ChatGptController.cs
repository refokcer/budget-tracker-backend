using budget_tracker_backend.Dto.ChatGpt;
using budget_tracker_backend.Services.ChatGpt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatGptController : ControllerBase
{
    private readonly IChatGptService _chatGptService;

    public ChatGptController(IChatGptService chatGptService)
    {
        _chatGptService = chatGptService;
    }

    [HttpPost("ask")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Ask([FromBody] ChatGptRequest request, CancellationToken cancellationToken)
    {
        var response = await _chatGptService.AskAsync(request, cancellationToken);
        return Ok(response);
    }
}
