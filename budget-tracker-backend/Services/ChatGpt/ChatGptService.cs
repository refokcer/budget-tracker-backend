using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using budget_tracker_backend.Dto.ChatGpt;

namespace budget_tracker_backend.Services.ChatGpt;

public class ChatGptService : IChatGptService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ChatGptService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> AskAsync(ChatGptRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey не настроен. Укажите ключ в user secrets.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        var endpoint = baseUrl.TrimEnd('/') + "/chat/completions";
        var model = request.Model ?? _configuration["OpenAI:DefaultModel"] ?? "gpt-3.5-turbo";
        var prompt = BuildPrompt(request);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var message = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return message ?? string.Empty;
    }

    private static string BuildPrompt(ChatGptRequest request)
    {
        var builder = new StringBuilder(request.Instruction.Trim());
        if (!string.IsNullOrWhiteSpace(request.Data))
        {
            builder.AppendLine().Append("Данные: ").AppendLine(request.Data);
        }
        if (request.Keywords != null && request.Keywords.Any())
        {
            builder.AppendLine().Append("Ключевые слова: ").Append(string.Join(", ", request.Keywords));
        }
        if (!string.IsNullOrWhiteSpace(request.ResponseFormat))
        {
            builder.AppendLine().Append("Ответь в формате: ").Append(request.ResponseFormat);
        }
        return builder.ToString();
    }
}
