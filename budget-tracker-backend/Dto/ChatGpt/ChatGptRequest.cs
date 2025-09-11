namespace budget_tracker_backend.Dto.ChatGpt;

public class ChatGptRequest
{
    /// <summary>Основная инструкция для чата.</summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>Дополнительные данные, которые нужно передать модели.</summary>
    public string? Data { get; set; }

    /// <summary>Ключевые слова, которые должны быть использованы в ответе.</summary>
    public IEnumerable<string>? Keywords { get; set; }

    /// <summary>Желаемый формат ответа (например, "json", "markdown").</summary>
    public string? ResponseFormat { get; set; }

    /// <summary>
    /// Модель OpenAI, например "gpt-3.5-turbo". Если не указана, используется значение по умолчанию из конфигурации.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>Температура генерации (0-2).</summary>
    public double? Temperature { get; set; }

    /// <summary>Максимальное количество токенов в ответе.</summary>
    public int? MaxTokens { get; set; }
}
