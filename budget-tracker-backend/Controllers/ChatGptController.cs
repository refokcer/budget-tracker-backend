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

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatGptController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IChatGptService _chatGptService;
    private readonly IApplicationDbContext _dbContext;

    public ChatGptController(IChatGptService chatGptService, IApplicationDbContext dbContext)
    {
        _chatGptService = chatGptService;
        _dbContext = dbContext;
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
            Temperature = 0,
            MaxTokens = CalculateMaxTokens(normalizedText)
        };

        var response = await _chatGptService.AskAsync(request, cancellationToken);

        PreparedTransactionDto[] transactions;
        try
        {
            var envelope = JsonSerializer.Deserialize<TransactionsEnvelope>(response, JsonOptions);
            transactions = envelope?.Transactions ?? throw new JsonException("transactions");
        }
        catch (JsonException)
        {
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

Верни результат строго в виде корректного JSON без каких-либо пояснений вне JSON. Корневой объект должен иметь единственное поле "transactions" с массивом транзакций. Каждая транзакция обязана соответствовать полям DTO PreparedTransactionDto:
- "title" — 1-4 слова, коротко описывающих операцию. Не используй лишние символы.
- "amount" — положительное десятичное число с точкой в качестве разделителя.
- "currencyId" — идентификатор валюты из userContext.currencies. Используй только существующие ID.
- "accountFrom" — ID счёта из userContext.accounts, с которого списаны деньги. Для расходов и трансферов обязателен, для доходов всегда null.
- "accountTo" — ID счёта из userContext.accounts, на который зачислены деньги. Для доходов и трансферов обязателен, для расходов всегда null.
- "budgetPlanId" — ID бюджета, если операция явно относится к одному из userContext.budgetPlans или логично совпадает с userContext.budgetPlanItems, иначе null.
- "categoryId" — ID категории из подходящей группы: расходы берут ID из userContext.categories.expense, доходы — из userContext.categories.income, трансферы — из userContext.categories.transfer. Если нет точного соответствия, укажи null.
- "date" — дата операции в формате ISO 8601 YYYY-MM-DDTHH:MM:SS (используй 00:00:00 если времени нет).
- "type" — строго одно из значений: "Expense", "Income" или "Transaction". Expense = расход, Income = доход, Transaction = перевод между счетами пользователя.
- "description" — краткий текст до 1-2 предложений, поясняющий операцию. Если нечего добавить — null.
- "authCode" — код авторизации платежа или аналогичный уникальный идентификатор из документа, если он есть. Иначе null.

Правила обработки:
1. Опираться на сведения из userContext для сопоставления валют, счетов, категорий, бюджетов. Не выдумывай новые значения и не создавай новые категории.
2. Суммы всегда положительные. Для расходов и переводов показывай абсолютное значение списания, для доходов — сумму поступления.
3. Уважай тип операции: расход уменьшает счёт accountFrom, доход увеличивает счёт accountTo, перевод перемещает средства между двумя счетами пользователя.
4. Не добавляй операции, которых нет в документе. Игнорируй служебные строки, заголовки, итоги и дубликаты (например, совпадающие по сумме, дате и authCode).
5. Если в документе встречается валюта, которой нет у пользователя, пропусти такие строки.
6. Если уверенности в категории нет — оставь categoryId равным null, но всё равно задай корректный type.
7. Расположи транзакции в хронологическом порядке (от ранних к поздним).
8. Если транзакций нет, верни {"transactions":[]}.

Ответ должен быть строго JSON, без комментариев и пояснений. Пример структуры:
{"transactions":[{"title":"Оплата кафе","amount":123.45,"currencyId":1,"accountFrom":2,"accountTo":null,"budgetPlanId":null,"categoryId":5,"date":"2024-01-15T00:00:00","type":"Expense","description":"ужин в кафе","authCode":"123456"}]}
""";
    }

    private static int CalculateMaxTokens(string text)
    {
        const int minTokens = 1024;
        const int maxTokens = 4000;

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
