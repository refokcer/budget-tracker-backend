using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    [HttpPost("parse")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> ParsePdf([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty");
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var document = PdfDocument.Open(stream);
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            text.AppendLine(page.Text);
        }

        return Ok(text.ToString());
    }
}

