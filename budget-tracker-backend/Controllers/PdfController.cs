using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using budget_tracker_backend.Dto.Pdf;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Services.ChatGpt;
using budget_tracker_backend.Services.Transactions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IChatGptService _chatGptService;
    private readonly ITransactionManager _transactionManager;
    private readonly ICategoryManager _categoryManager;

    public PdfController(
        IChatGptService chatGptService,
        ITransactionManager transactionManager,
        ICategoryManager categoryManager)
    {
        _chatGptService = chatGptService;
        _transactionManager = transactionManager;
        _categoryManager = categoryManager;
    }

    [HttpPost("parse")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<IEnumerable<PreparedTransactionDto>>> ParsePdf(
        [FromForm] ParsePdfRequest request,
        CancellationToken ct)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
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

        var categories = await _categoryManager.GetAllAsync(ct);
        var instruction =
            "Извлеки из текста банковской выписки все транзакции. " +
            "Верни массив JSON объектов со свойствами Title, Amount, Currency, Date, Type (Income/Expense), Category, Description, AuthCode.";

        var data = $"Категории пользователя: {string.Join(", ", categories.Select(c => c.Title))}. Выписка:\n{text}";
        var chatRequest = new ChatGptRequest
        {
            Instruction = instruction,
            Data = data,
            ResponseFormat = "json",
            Temperature = 0.2
        };

        var aiResponse = await _chatGptService.AskAsync(chatRequest, ct);

        List<ImportTransactionDto>? imported;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            imported = JsonSerializer.Deserialize<List<ImportTransactionDto>>(aiResponse, options);
        }
        catch (JsonException)
        {
            return BadRequest("Failed to parse AI response");
        }

        if (imported == null)
            return Ok(Array.Empty<PreparedTransactionDto>());

        var prepared = await _transactionManager.PrepareAsync(imported, ct);
        return Ok(prepared);
    }
}

