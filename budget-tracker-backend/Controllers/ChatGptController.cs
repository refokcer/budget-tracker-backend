using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.ChatGpt;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Dto.Transactions;
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
            return BadRequest("Файл PDF не передан или пуст.");
        }

        if (!form.Pdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Ожидается файл с расширением .pdf.");
        }

        string rawText;
        try
        {
            rawText = await ExtractPdfTextAsync(form.Pdf, cancellationToken);
        }
        catch (Exception ex) when (ex is PdfException or InvalidOperationException or IOException)
        {
            _logger.LogError(ex, "Failed to extract text from PDF {FileName}", form.Pdf.FileName);
            return BadRequest("Не удалось прочитать содержимое PDF файла.");
        }

        var normalizedText = NormalizeExtractedText(rawText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return BadRequest("В переданном PDF не найден текст для анализа.");
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
            return BadRequest("Ответ ChatGPT имеет неверный формат. Ожидается JSON с массивом transactions.");
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
            .Where(c => c.Type == TransactionCategoryType.Transaction)
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
Ты финансовый ассистент. Тебе передают текст банковских и платежных выписок вместе со справочными данными пользователя (списки его счетов, валют, категорий, бюджетных планов). Справочная информация находится в объекте userContext: accounts (счета пользователя), currencies (валюты), categories (подмассивы expense/income/transfer/uncategorized), budgetPlans и budgetPlanItems. Сам документ передан в объекте document: текст в поле text, имя файла в поле name. Проанализируй данные и выдели только реальные финансовые операции пользователя.

Ответ формируй строго как корректный JSON-документ UTF-8 без BOM, без предваряющего текста и комментариев. Корневой объект должен содержать единственное поле "transactions". Значение — массив объектов, каждый объект представляет PreparedTransactionDto с полями и типами:
{
  "title": string (1-4 слова, обязательно включи название магазина/получателя и, если возможно, уточнение типа операции: продукты, услуги, техника, подписка и т.д.; избегай общих формулировок вроде "Покупка" без уточнения),
  "amount": number (положительное десятичное число, разделитель — точка, не заключать в кавычки),
  "currencyId": integer (используй существующие ID из userContext.currencies),
  "accountFrom": integer | null (обязателен для расходов и переводов, для доходов всегда null),
  "accountTo": integer | null (обязателен для доходов и переводов, для расходов всегда null),
  "budgetPlanId": integer | null (используй ID из userContext.budgetPlans, иначе null),
  "categoryId": integer | null (для расходов — ID из userContext.categories.expense, для доходов — из income, для переводов — из transfer; заполняй ID при любой разумной уверенности, null только при полном отсутствии данных),
  "date": string (формат ISO 8601 YYYY-MM-DDTHH:MM:SS; если время отсутствует, ставь 00:00:00),
  "type": string (строго одно из: "Expense", "Income", "Transaction"),
  "description": string | null (до 1-2 предложений, null если нечего добавить),
  "authCode": string | null (укажи код авторизации/уникальный идентификатор, если есть, иначе null)
}
Все поля в каждом объекте должны присутствовать. Используй только двойные кавычки, не оставляй лишних запятых. Значения null пиши без кавычек. Не добавляй другие поля.

Правила обработки:
1. Опираться на сведения из userContext для сопоставления валют, счетов, категорий, бюджетов. Не выдумывай новые значения и не создавай новые категории.
2. Суммы всегда положительные. Для расходов и переводов показывай абсолютное значение списания, для доходов — сумму поступления.
3. Уважай тип операции: расход уменьшает счёт accountFrom, доход увеличивает счёт accountTo, перевод перемещает средства между двумя счетами пользователя.
4. Не добавляй операции, которых нет в документе. Игнорируй служебные строки, заголовки, итоги и дубликаты (например, совпадающие по сумме, дате и authCode).
5. Если в документе встречается валюта, которой нет у пользователя, пропусти такие строки.
6. Активно сопоставляй операции с категориями пользователя: используй название магазина, описание и тип услуги/товара. Ставь categoryId, когда есть хоть какая-то разумная уверенность. Оставляй null только если категоризировать невозможно.
7. Расположи транзакции в хронологическом порядке (от ранних к поздним).
8. Если транзакций нет, верни {"transactions":[]}.

Строго придерживайся структуры. Пример допустимого ответа:
{"transactions":[{"title":"Продукты Магнит","amount":1234.56,"currencyId":1,"accountFrom":2,"accountTo":null,"budgetPlanId":null,"categoryId":5,"date":"2024-01-15T00:00:00","type":"Expense","description":"Покупка продуктов в Магните","authCode":"123456"}]}
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
