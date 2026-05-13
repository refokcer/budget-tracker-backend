using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.ChatGpt;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Extensions;
using budget_tracker_backend.Services.ChatGpt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Exceptions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatGptController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions;

    static ChatGptController()
    {
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        JsonOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }

    private readonly IChatGptService _chatGptService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ChatGptController> _logger;

    public ChatGptController(IChatGptService chatGptService, IApplicationDbContext dbContext, ILogger<ChatGptController> logger)
    {
        _chatGptService = chatGptService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("ask")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Ask([FromBody] ChatGptRequest request, CancellationToken cancellationToken)
    {
        var response = await _chatGptService.AskAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("parse-transactions")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PreparedTransactionDto[]>> ParseTransactionsFromPdf([FromForm] ParseTransactionsRequest form, CancellationToken cancellationToken)
    {
        if (form.Pdf == null || form.Pdf.Length == 0)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, "missing_pdf_file", "PDF file is required.");
        }

        if (!form.Pdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return this.ApiError(StatusCodes.Status400BadRequest, "invalid_pdf_file", "Expected a file with .pdf extension.");
        }

        string rawText;
        try
        {
            rawText = await ExtractPdfTextAsync(form.Pdf, cancellationToken);
        }
        catch (Exception ex) when (ex is PdfException or InvalidOperationException or IOException)
        {
            _logger.LogError(ex, "Failed to extract text from PDF {FileName}", form.Pdf.FileName);
            return this.ApiError(StatusCodes.Status400BadRequest, "pdf_read_failed", "Failed to read PDF content.");
        }

        var normalizedText = NormalizeExtractedText(rawText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return this.ApiError(StatusCodes.Status400BadRequest, "empty_pdf_text", "No text was found in the PDF for analysis.");
        }

        var userContext = await BuildUserContextAsync(cancellationToken);
        var instruction = BuildPdfImportInstruction();

        var payload = new
        {
            document = new
            {
                name = form.Pdf.FileName,
                text = normalizedText
            },
            userContext
        };

        var request = new ChatGptRequest
        {
            Instruction = instruction,
            Data = JsonSerializer.Serialize(payload, JsonOptions),
            ResponseFormat = "json",
            Temperature = 0.5,
            MaxTokens = CalculateMaxTokens(normalizedText)
        };

        var response = await _chatGptService.AskAsync(request, cancellationToken);

        PreparedTransactionDto[] transactions;
        try
        {
            var envelope = JsonSerializer.Deserialize<TransactionsEnvelope>(response, JsonOptions);
            transactions = envelope?.Transactions ?? throw new JsonException("transactions");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize ChatGPT response: {Response}", response);
            return this.ApiError(StatusCodes.Status400BadRequest, "invalid_ai_response", "ChatGPT response has invalid format. Expected JSON with transactions array.");
        }

        return Ok(transactions);
    }

    private static async Task<string> ExtractPdfTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);

        var pdfBytes = memory.ToArray();
        using var pdfStream = new MemoryStream(pdfBytes, writable: false);
        using var reader = new PdfReader(pdfStream);
        reader.SetUnethicalReading(true);
        using var document = new PdfDocument(reader);

        var builder = new StringBuilder();

        for (var pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine($"--- page {pageNumber} ---");

            var strategy = new LocationTextExtractionStrategy();
            var page = document.GetPage(pageNumber);
            var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                builder.AppendLine(pageText.TrimEnd());
            }
        }

        return builder.ToString();
    }

    private static string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (char.IsControl(ch) && ch != '\n' && ch != '\t')
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }

    private async Task<UserFinanceContext> BuildUserContextAsync(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.Title, c.Type })
            .ToListAsync(cancellationToken);

        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Select(a => new AccountPromptItem(
                a.Id,
                a.Title,
                a.CurrencyId,
                a.Currency != null ? a.Currency.Code : null,
                a.Description))
            .ToArrayAsync(cancellationToken);

        var currencies = await _dbContext.Currencies
            .AsNoTracking()
            .Select(c => new CurrencyPromptItem(
                c.Id,
                c.Code,
                c.Title,
                c.Symbol.ToString(),
                c.IsBase))
            .ToArrayAsync(cancellationToken);

        var budgetPlans = await _dbContext.BudgetPlans
            .AsNoTracking()
            .Select(p => new BudgetPlanPromptItem(
                p.Id,
                p.Title,
                p.Type.ToString(),
                p.StartDate,
                p.EndDate,
                p.ParentId))
            .ToArrayAsync(cancellationToken);

        var budgetPlanItems = await _dbContext.BudgetPlanItems
            .AsNoTracking()
            .Select(i => new BudgetPlanItemPromptItem(
                i.Id,
                i.BudgetPlanId,
                i.CategoryId,
                i.CurrencyId,
                i.Amount,
                i.Description))
            .ToArrayAsync(cancellationToken);

        var expenseCategories = categories
            .Where(c => c.Type == TransactionCategoryType.Expense)
            .Select(c => new CategoryPromptItem(c.Id, c.Title))
            .ToArray();

        var incomeCategories = categories
            .Where(c => c.Type == TransactionCategoryType.Income)
            .Select(c => new CategoryPromptItem(c.Id, c.Title))
            .ToArray();

        var transferCategories = categories
            .Where(c => c.Type == TransactionCategoryType.Transfer)
            .Select(c => new CategoryPromptItem(c.Id, c.Title))
            .ToArray();

        var uncategorizedCategories = categories
            .Where(c => c.Type == TransactionCategoryType.None)
            .Select(c => new CategoryPromptItem(c.Id, c.Title))
            .ToArray();

        return new UserFinanceContext(
            accounts,
            currencies,
            new CategoriesPayload(
                expenseCategories,
                incomeCategories,
                transferCategories,
                uncategorizedCategories),
            budgetPlans,
            budgetPlanItems);
    }

    private static string BuildPdfImportInstruction()
    {
        return """
РўС‹ С„РёРЅР°РЅСЃРѕРІС‹Р№ Р°СЃСЃРёСЃС‚РµРЅС‚. РўРµР±Рµ РїРµСЂРµРґР°СЋС‚ С‚РµРєСЃС‚ Р±Р°РЅРєРѕРІСЃРєРёС… Рё РїР»Р°С‚РµР¶РЅС‹С… РІС‹РїРёСЃРѕРє РІРјРµСЃС‚Рµ СЃРѕ СЃРїСЂР°РІРѕС‡РЅС‹РјРё РґР°РЅРЅС‹РјРё РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ (СЃРїРёСЃРєРё РµРіРѕ СЃС‡РµС‚РѕРІ, РІР°Р»СЋС‚, РєР°С‚РµРіРѕСЂРёР№, Р±СЋРґР¶РµС‚РЅС‹С… РїР»Р°РЅРѕРІ). РЎРїСЂР°РІРѕС‡РЅР°СЏ РёРЅС„РѕСЂРјР°С†РёСЏ РЅР°С…РѕРґРёС‚СЃСЏ РІ РѕР±СЉРµРєС‚Рµ userContext: accounts (СЃС‡РµС‚Р° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ), currencies (РІР°Р»СЋС‚С‹), categories (РїРѕРґРјР°СЃСЃРёРІС‹ expense/income/transfer/uncategorized), budgetPlans Рё budgetPlanItems. РЎР°Рј РґРѕРєСѓРјРµРЅС‚ РїРµСЂРµРґР°РЅ РІ РѕР±СЉРµРєС‚Рµ document: С‚РµРєСЃС‚ РІ РїРѕР»Рµ text, РёРјСЏ С„Р°Р№Р»Р° РІ РїРѕР»Рµ name. РџСЂРѕР°РЅР°Р»РёР·РёСЂСѓР№ РґР°РЅРЅС‹Рµ Рё РІС‹РґРµР»Рё С‚РѕР»СЊРєРѕ СЂРµР°Р»СЊРЅС‹Рµ С„РёРЅР°РЅСЃРѕРІС‹Рµ РѕРїРµСЂР°С†РёРё РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ.

РћС‚РІРµС‚ С„РѕСЂРјРёСЂСѓР№ СЃС‚СЂРѕРіРѕ РєР°Рє РєРѕСЂСЂРµРєС‚РЅС‹Р№ JSON-РґРѕРєСѓРјРµРЅС‚ UTF-8 Р±РµР· BOM, Р±РµР· РїСЂРµРґРІР°СЂСЏСЋС‰РµРіРѕ С‚РµРєСЃС‚Р° Рё РєРѕРјРјРµРЅС‚Р°СЂРёРµРІ. РљРѕСЂРЅРµРІРѕР№ РѕР±СЉРµРєС‚ РґРѕР»Р¶РµРЅ СЃРѕРґРµСЂР¶Р°С‚СЊ РµРґРёРЅСЃС‚РІРµРЅРЅРѕРµ РїРѕР»Рµ "transactions". Р—РЅР°С‡РµРЅРёРµ вЂ” РјР°СЃСЃРёРІ РѕР±СЉРµРєС‚РѕРІ, РєР°Р¶РґС‹Р№ РѕР±СЉРµРєС‚ РїСЂРµРґСЃС‚Р°РІР»СЏРµС‚ PreparedTransactionDto СЃ РїРѕР»СЏРјРё Рё С‚РёРїР°РјРё:
{
  "title": string (1-4 СЃР»РѕРІР°, РѕР±СЏР·Р°С‚РµР»СЊРЅРѕ РІРєР»СЋС‡Рё РЅР°Р·РІР°РЅРёРµ РјР°РіР°Р·РёРЅР°/РїРѕР»СѓС‡Р°С‚РµР»СЏ Рё, РµСЃР»Рё РІРѕР·РјРѕР¶РЅРѕ, СѓС‚РѕС‡РЅРµРЅРёРµ С‚РёРїР° РѕРїРµСЂР°С†РёРё: РїСЂРѕРґСѓРєС‚С‹, СѓСЃР»СѓРіРё, С‚РµС…РЅРёРєР°, РїРѕРґРїРёСЃРєР° Рё С‚.Рґ.; РёР·Р±РµРіР°Р№ РѕР±С‰РёС… С„РѕСЂРјСѓР»РёСЂРѕРІРѕРє РІСЂРѕРґРµ "РџРѕРєСѓРїРєР°" Р±РµР· СѓС‚РѕС‡РЅРµРЅРёСЏ),
  "amount": number (РїРѕР»РѕР¶РёС‚РµР»СЊРЅРѕРµ РґРµСЃСЏС‚РёС‡РЅРѕРµ С‡РёСЃР»Рѕ, СЂР°Р·РґРµР»РёС‚РµР»СЊ вЂ” С‚РѕС‡РєР°, РЅРµ Р·Р°РєР»СЋС‡Р°С‚СЊ РІ РєР°РІС‹С‡РєРё),
  "currencyId": integer (РёСЃРїРѕР»СЊР·СѓР№ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ ID РёР· userContext.currencies),
  "accountFrom": integer | null (РѕР±СЏР·Р°С‚РµР»РµРЅ РґР»СЏ СЂР°СЃС…РѕРґРѕРІ Рё РїРµСЂРµРІРѕРґРѕРІ, РґР»СЏ РґРѕС…РѕРґРѕРІ РІСЃРµРіРґР° null),
  "accountTo": integer | null (РѕР±СЏР·Р°С‚РµР»РµРЅ РґР»СЏ РґРѕС…РѕРґРѕРІ Рё РїРµСЂРµРІРѕРґРѕРІ, РґР»СЏ СЂР°СЃС…РѕРґРѕРІ РІСЃРµРіРґР° null),
  "budgetPlanId": integer | null (РёСЃРїРѕР»СЊР·СѓР№ ID РёР· userContext.budgetPlans, РёРЅР°С‡Рµ null),
  "categoryId": integer | null (РґР»СЏ СЂР°СЃС…РѕРґРѕРІ вЂ” ID РёР· userContext.categories.expense, РґР»СЏ РґРѕС…РѕРґРѕРІ вЂ” РёР· income, РґР»СЏ РїРµСЂРµРІРѕРґРѕРІ вЂ” РёР· transfer; Р·Р°РїРѕР»РЅСЏР№ ID РїСЂРё Р»СЋР±РѕР№ СЂР°Р·СѓРјРЅРѕР№ СѓРІРµСЂРµРЅРЅРѕСЃС‚Рё, null С‚РѕР»СЊРєРѕ РїСЂРё РїРѕР»РЅРѕРј РѕС‚СЃСѓС‚СЃС‚РІРёРё РґР°РЅРЅС‹С…),
  "date": string (С„РѕСЂРјР°С‚ ISO 8601 YYYY-MM-DDTHH:MM:SS; РµСЃР»Рё РІСЂРµРјСЏ РѕС‚СЃСѓС‚СЃС‚РІСѓРµС‚, СЃС‚Р°РІСЊ 00:00:00),
  "type": string (СЃС‚СЂРѕРіРѕ РѕРґРЅРѕ РёР·: "Expense", "Income", "Transaction"),
  "description": string | null (РґРѕ 1-2 РїСЂРµРґР»РѕР¶РµРЅРёР№, null РµСЃР»Рё РЅРµС‡РµРіРѕ РґРѕР±Р°РІРёС‚СЊ),
  "authCode": string | null (СѓРєР°Р¶Рё РєРѕРґ Р°РІС‚РѕСЂРёР·Р°С†РёРё/СѓРЅРёРєР°Р»СЊРЅС‹Р№ РёРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ, РµСЃР»Рё РµСЃС‚СЊ, РёРЅР°С‡Рµ null)
}
Р’СЃРµ РїРѕР»СЏ РІ РєР°Р¶РґРѕРј РѕР±СЉРµРєС‚Рµ РґРѕР»Р¶РЅС‹ РїСЂРёСЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ. РСЃРїРѕР»СЊР·СѓР№ С‚РѕР»СЊРєРѕ РґРІРѕР№РЅС‹Рµ РєР°РІС‹С‡РєРё, РЅРµ РѕСЃС‚Р°РІР»СЏР№ Р»РёС€РЅРёС… Р·Р°РїСЏС‚С‹С…. Р—РЅР°С‡РµРЅРёСЏ null РїРёС€Рё Р±РµР· РєР°РІС‹С‡РµРє. РќРµ РґРѕР±Р°РІР»СЏР№ РґСЂСѓРіРёРµ РїРѕР»СЏ.

РџСЂР°РІРёР»Р° РѕР±СЂР°Р±РѕС‚РєРё:
1. РћРїРёСЂР°С‚СЊСЃСЏ РЅР° СЃРІРµРґРµРЅРёСЏ РёР· userContext РґР»СЏ СЃРѕРїРѕСЃС‚Р°РІР»РµРЅРёСЏ РІР°Р»СЋС‚, СЃС‡РµС‚РѕРІ, РєР°С‚РµРіРѕСЂРёР№, Р±СЋРґР¶РµС‚РѕРІ. РќРµ РІС‹РґСѓРјС‹РІР°Р№ РЅРѕРІС‹Рµ Р·РЅР°С‡РµРЅРёСЏ Рё РЅРµ СЃРѕР·РґР°РІР°Р№ РЅРѕРІС‹Рµ РєР°С‚РµРіРѕСЂРёРё.
2. РЎСѓРјРјС‹ РІСЃРµРіРґР° РїРѕР»РѕР¶РёС‚РµР»СЊРЅС‹Рµ. Р”Р»СЏ СЂР°СЃС…РѕРґРѕРІ Рё РїРµСЂРµРІРѕРґРѕРІ РїРѕРєР°Р·С‹РІР°Р№ Р°Р±СЃРѕР»СЋС‚РЅРѕРµ Р·РЅР°С‡РµРЅРёРµ СЃРїРёСЃР°РЅРёСЏ, РґР»СЏ РґРѕС…РѕРґРѕРІ вЂ” СЃСѓРјРјСѓ РїРѕСЃС‚СѓРїР»РµРЅРёСЏ.
3. РЈРІР°Р¶Р°Р№ С‚РёРї РѕРїРµСЂР°С†РёРё: СЂР°СЃС…РѕРґ СѓРјРµРЅСЊС€Р°РµС‚ СЃС‡С‘С‚ accountFrom, РґРѕС…РѕРґ СѓРІРµР»РёС‡РёРІР°РµС‚ СЃС‡С‘С‚ accountTo, РїРµСЂРµРІРѕРґ РїРµСЂРµРјРµС‰Р°РµС‚ СЃСЂРµРґСЃС‚РІР° РјРµР¶РґСѓ РґРІСѓРјСЏ СЃС‡РµС‚Р°РјРё РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ.
4. РќРµ РґРѕР±Р°РІР»СЏР№ РѕРїРµСЂР°С†РёРё, РєРѕС‚РѕСЂС‹С… РЅРµС‚ РІ РґРѕРєСѓРјРµРЅС‚Рµ. РРіРЅРѕСЂРёСЂСѓР№ СЃР»СѓР¶РµР±РЅС‹Рµ СЃС‚СЂРѕРєРё, Р·Р°РіРѕР»РѕРІРєРё, РёС‚РѕРіРё Рё РґСѓР±Р»РёРєР°С‚С‹ (РЅР°РїСЂРёРјРµСЂ, СЃРѕРІРїР°РґР°СЋС‰РёРµ РїРѕ СЃСѓРјРјРµ, РґР°С‚Рµ Рё authCode).
5. Р•СЃР»Рё РІ РґРѕРєСѓРјРµРЅС‚Рµ РІСЃС‚СЂРµС‡Р°РµС‚СЃСЏ РІР°Р»СЋС‚Р°, РєРѕС‚РѕСЂРѕР№ РЅРµС‚ Сѓ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ, РїСЂРѕРїСѓСЃС‚Рё С‚Р°РєРёРµ СЃС‚СЂРѕРєРё.
6. РђРєС‚РёРІРЅРѕ СЃРѕРїРѕСЃС‚Р°РІР»СЏР№ РѕРїРµСЂР°С†РёРё СЃ РєР°С‚РµРіРѕСЂРёСЏРјРё РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: РёСЃРїРѕР»СЊР·СѓР№ РЅР°Р·РІР°РЅРёРµ РјР°РіР°Р·РёРЅР°, РѕРїРёСЃР°РЅРёРµ Рё С‚РёРї СѓСЃР»СѓРіРё/С‚РѕРІР°СЂР°. РЎС‚Р°РІСЊ categoryId, РєРѕРіРґР° РµСЃС‚СЊ С…РѕС‚СЊ РєР°РєР°СЏ-С‚Рѕ СЂР°Р·СѓРјРЅР°СЏ СѓРІРµСЂРµРЅРЅРѕСЃС‚СЊ. РћСЃС‚Р°РІР»СЏР№ null С‚РѕР»СЊРєРѕ РµСЃР»Рё РєР°С‚РµРіРѕСЂРёР·РёСЂРѕРІР°С‚СЊ РЅРµРІРѕР·РјРѕР¶РЅРѕ.
7. Р Р°СЃРїРѕР»РѕР¶Рё С‚СЂР°РЅР·Р°РєС†РёРё РІ С…СЂРѕРЅРѕР»РѕРіРёС‡РµСЃРєРѕРј РїРѕСЂСЏРґРєРµ (РѕС‚ СЂР°РЅРЅРёС… Рє РїРѕР·РґРЅРёРј).
8. Р•СЃР»Рё С‚СЂР°РЅР·Р°РєС†РёР№ РЅРµС‚, РІРµСЂРЅРё {"transactions":[]}.

РЎС‚СЂРѕРіРѕ РїСЂРёРґРµСЂР¶РёРІР°Р№СЃСЏ СЃС‚СЂСѓРєС‚СѓСЂС‹. РџСЂРёРјРµСЂ РґРѕРїСѓСЃС‚РёРјРѕРіРѕ РѕС‚РІРµС‚Р°:
{"transactions":[{"title":"РџСЂРѕРґСѓРєС‚С‹ РњР°РіРЅРёС‚","amount":1234.56,"currencyId":1,"accountFrom":2,"accountTo":null,"budgetPlanId":null,"categoryId":5,"date":"2024-01-15T00:00:00","type":"Expense","description":"РџРѕРєСѓРїРєР° РїСЂРѕРґСѓРєС‚РѕРІ РІ РњР°РіРЅРёС‚Рµ","authCode":"123456"}]}
""";

    }

    private static int CalculateMaxTokens(string text)
    {
        const int minTokens = 1024;
        const int maxTokens = 400000;

        if (string.IsNullOrWhiteSpace(text))
        {
            return minTokens;
        }

        var approxTokens = text.Length / 3;
        var withBuffer = approxTokens + 512;

        return Math.Clamp(withBuffer, minTokens, maxTokens);
    }

    private sealed record UserFinanceContext(
        AccountPromptItem[] Accounts,
        CurrencyPromptItem[] Currencies,
        CategoriesPayload Categories,
        BudgetPlanPromptItem[] BudgetPlans,
        BudgetPlanItemPromptItem[] BudgetPlanItems);

    private sealed record CategoriesPayload(
        CategoryPromptItem[] Expense,
        CategoryPromptItem[] Income,
        CategoryPromptItem[] Transfer,
        CategoryPromptItem[] Uncategorized);

    private sealed record AccountPromptItem(int Id, string Title, int CurrencyId, string? CurrencyCode, string? Description);

    private sealed record CurrencyPromptItem(int Id, string Code, string Title, string Symbol, bool IsBase);

    private sealed record CategoryPromptItem(int Id, string Title);

    private sealed record BudgetPlanPromptItem(int Id, string Title, string Type, DateTime StartDate, DateTime EndDate, int? ParentId);

    private sealed record BudgetPlanItemPromptItem(int Id, int BudgetPlanId, int CategoryId, int CurrencyId, decimal Amount, string? Description);

    private sealed record TransactionsEnvelope(PreparedTransactionDto[] Transactions);
}

