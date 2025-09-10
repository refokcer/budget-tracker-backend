using budget_tracker_backend.Dto.ChatGpt;

namespace budget_tracker_backend.Services.ChatGpt;

public interface IChatGptService
{
    /// <summary>
    /// Отправляет запрос к модели ChatGPT и возвращает текст ответа.
    /// </summary>
    Task<string> AskAsync(ChatGptRequest request, CancellationToken cancellationToken = default);
}
