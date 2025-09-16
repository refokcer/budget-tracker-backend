using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using budget_tracker_backend.Dto.Pdf;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Services.ChatGpt;
using budget_tracker_backend.Services.Transactions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private static readonly string[] SupportedDateFormats =
    {
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd.MM.yy",
        "dd/MM/yyyy",
        "dd/MM/yy",
        "dd-MM-yyyy",
        "dd-MM-yy"
    };

    private static readonly CultureInfo[] DateCultures =
    {
        CultureInfo.InvariantCulture,
        new("uk-UA"),
        new("ru-RU"),
        CultureInfo.GetCultureInfo("en-GB")
    };

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

        for (var i = 1; i <= document.GetNumberOfPages(); i++)
        {
            var page = document.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            text.AppendLine(PdfTextExtractor.GetTextFromPage(page, strategy));
        }

        var statementText = text.ToString();

        var expenses = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Expense, ct);
        var incomes = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Income, ct);
        var transfers = await _categoryManager.GetByTypeAsync(TransactionCategoryType.Transaction, ct);

        var instruction = """
Ты финансовый ассистент, который превращает текст банковской выписки в структурированные данные пользователя.
1. Проанализируй выписку и найди все операции.
2. Для каждой операции сформируй один JSON-объект.

Требования к полям объекта:
- "Title" — краткое название из 1–4 слов, отражающее суть операции.
- "Amount" — число с точностью до копеек. Расходы и комиссии делай отрицательными, доходы положительными, переводы указывай со знаком, который соответствует направлению операции.
- "Currency" — буквенный код валюты (например, UAH, USD).
- "Date" — дата операции в формате YYYY-MM-DD.
- "Type" — одно из значений: Income (зачисления), Expense (расходы и комиссии), Transaction (переводы между счетами).
- "Category" — выбери подходящую категорию из списка категорий соответствующего типа. Если подходящей нет, верни пустую строку.
- "Description" — полное описание операции из выписки.
- "AuthCode" — код авторизации из выписки или null, если его нет.
- "AccountFrom" и "AccountTo" — указывай только если по тексту однозначно понятно, какие счета задействованы. Иначе верни null.

Верни строго JSON-массив без дополнительных комментариев. Если операций нет — верни []
""";

        var dataBuilder = new StringBuilder();
        AppendCategoryList(dataBuilder, "Категории расходов", expenses);
        AppendCategoryList(dataBuilder, "Категории доходов", incomes);
        AppendCategoryList(dataBuilder, "Категории переводов", transfers);
        dataBuilder.AppendLine("Текст выписки:").AppendLine(statementText);

        var chatRequest = new ChatGptRequest
        {
            Instruction = instruction,
            Data = dataBuilder.ToString(),
            ResponseFormat = "json",
            Temperature = 0,
            MaxTokens = CalculateMaxTokens(statementText.Length)
        };

        var aiResponse = await _chatGptService.AskAsync(chatRequest, ct);

        List<AiParsedTransactionDto>? imported;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            imported = JsonSerializer.Deserialize<List<AiParsedTransactionDto>>(aiResponse, options);
        }
        catch (JsonException)
        {
            return BadRequest("Failed to parse AI response");
        }

        if (imported == null)
            return Ok(Array.Empty<PreparedTransactionDto>());

        var normalized = new List<ImportTransactionDto>();
        foreach (var item in imported)
        {
            if (!TryNormalize(item, out var dto, out var error))
                return BadRequest(error);

            normalized.Add(dto);
        }

        if (normalized.Count == 0)
            return Ok(Array.Empty<PreparedTransactionDto>());

        var prepared = await _transactionManager.PrepareAsync(normalized, ct);
        return Ok(prepared);
    }

    private static void AppendCategoryList(StringBuilder builder, string title, IEnumerable<Category> categories)
    {
        var list = categories
            .Select(c => c.Title)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToArray();

        builder
            .Append(title)
            .Append(": ")
            .AppendLine(list.Length == 0 ? "(нет категорий)" : string.Join(", ", list));
    }

    private static int CalculateMaxTokens(int statementLength)
    {
        if (statementLength <= 0)
            return 512;

        var estimated = statementLength / 3;
        return Math.Clamp(estimated, 512, 6000);
    }

    private static bool TryNormalize(
        AiParsedTransactionDto source,
        out ImportTransactionDto result,
        out string? error)
    {
        result = null!;
        error = null;

        if (source == null)
        {
            error = "AI response contains null transaction entry.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(source.Title))
        {
            error = "Transaction title is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(source.Currency))
        {
            error = $"Currency is required for transaction '{source.Title}'.";
            return false;
        }

        if (!TryParseDate(source.Date, out var date))
        {
            error = $"Cannot parse date '{source.Date}' for transaction '{source.Title}'. Use YYYY-MM-DD format.";
            return false;
        }

        result = new ImportTransactionDto
        {
            Title = source.Title.Trim(),
            Amount = source.Amount,
            Currency = source.Currency.Trim(),
            AccountFrom = source.AccountFrom,
            AccountTo = source.AccountTo,
            Date = date,
            Type = source.Type,
            Category = string.IsNullOrWhiteSpace(source.Category) ? null : source.Category.Trim(),
            Description = source.Description,
            AuthCode = source.AuthCode
        };

        return true;
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParseExact(value, SupportedDateFormats, culture, DateTimeStyles.AssumeLocal, out date))
                return true;
        }

        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParse(value, culture, DateTimeStyles.AssumeLocal, out date))
                return true;
        }

        return false;
    }
}
