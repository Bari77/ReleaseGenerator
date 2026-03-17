# ReleaseGenerator API

ASP.NET Core API that parses a CHANGELOG and uses Ollama to generate release notes for multiple formats and languages. See the [root README](../README.md) for configuration, changelog format, and API usage.

## Run locally

```bash
dotnet run
```

Default URL: `http://localhost:5041` (see `Properties/launchSettings.json`). Ensure Ollama is running and that `ReleaseGenerator:Ollama:BaseUrl` and `ReleaseGenerator:Ollama:Model` match your setup.

## Build

```bash
dotnet build
```

## Project layout

- **Controllers**: `ReleaseNotesController` — endpoints for formats, languages, and generate (JSON and raw body).
- **Models**: Request/response and parsed changelog DTOs.
- **Services**: `ChangelogParser` (parse CHANGELOG markdown), `OllamaReleaseService` (call Ollama OpenAI-compatible chat API).
- **Configuration**: `ReleaseGeneratorOptions` bound from `appsettings.json` / environment.
