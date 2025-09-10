# budget-tracker-backend

## ChatGPT service

Service `ChatGptService` allows sending requests to OpenAI ChatGPT models.

### Configuration

Store OpenAI credentials using [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
cd budget-tracker-backend
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your_api_key"
dotnet user-secrets set "OpenAI:DefaultModel" "gpt-3.5-turbo"
```

### Usage

After configuring secrets, run the application and call the `POST /api/chatgpt/ask` endpoint via Swagger with a `ChatGptRequest` body to receive a response from ChatGPT.
If the endpoint returns `429 Too Many Requests`, your API key may have exhausted its quota or you are sending requests too quickly.
