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

## SonarQube test coverage

The test project already uses `coverlet.collector`, and this repository now includes:

- `coverage.runsettings` to force the `XPlat Code Coverage` collector to emit OpenCover output.
- `scripts/run-sonar-coverage.sh` to run the test suite and place coverage files under `TestResults/`.

### Generate coverage locally

```bash
./scripts/run-sonar-coverage.sh
```

After the command finishes, SonarQube-compatible coverage files will be created under:

```bash
TestResults/**/coverage.opencover.xml
```

### SonarScanner for .NET example

If you run SonarQube analysis manually or in CI, pass the OpenCover report path to the scanner:

```bash
dotnet sonarscanner begin \
  /k:"your_project_key" \
  /d:sonar.host.url="https://your-sonarqube.example.com" \
  /d:sonar.token="<your-token>" \
  /d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml"

dotnet build budget-tracker-backend.sln
./scripts/run-sonar-coverage.sh
dotnet sonarscanner end /d:sonar.token="<your-token>"
```

### Что нужно сделать вручную

- Установить `dotnet-sonarscanner`, если он ещё не установлен:

  ```bash
  dotnet tool install --global dotnet-sonarscanner
  ```

- Подставить свои значения для `your_project_key`, `sonar.host.url` и `sonar.token`.
- Добавить эти команды в ваш CI/CD пайплайн, если SonarQube должен считать coverage автоматически на каждом пуше или pull request.

