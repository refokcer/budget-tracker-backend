using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using budget_tracker_backend.Dto.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    [HttpPost("parse")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<string>> ParsePdf([FromForm] ParsePdfRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty");
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var text = new StringBuilder();
        using var reader = new PdfReader(stream);
        using var document = new PdfDocument(reader);

        for (int i = 1; i <= document.GetNumberOfPages(); i++)
        {
            var page = document.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            text.AppendLine(PdfTextExtractor.GetTextFromPage(page, strategy));
        }

        return Ok(text.ToString());
    }
}

