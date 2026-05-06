using budget_tracker_backend.Dto.AdminData;
using budget_tracker_backend.Services.AdminData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AdminDataController : ControllerBase
{
    private readonly IAdminDataManager _manager;

    public AdminDataController(IAdminDataManager manager)
    {
        _manager = manager;
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        await _manager.ClearCurrentUserDataAsync(HttpContext.RequestAborted);
        return Ok(new { cleared = true });
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] AdminDataImportDto dto)
    {
        var result = await _manager.ImportAsync(dto, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("sample")]
    public IActionResult Sample()
    {
        return Ok(_manager.BuildSample());
    }

    [HttpGet("templates")]
    public IActionResult Templates()
    {
        return Ok(_manager.GetTemplates());
    }

    [HttpGet("templates/{templateId}")]
    public async Task<IActionResult> Template(string templateId)
    {
        return Ok(await _manager.GetTemplateAsync(templateId, HttpContext.RequestAborted));
    }
}
