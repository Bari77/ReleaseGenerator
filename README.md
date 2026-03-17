# ReleaseGenerator

ReleaseGenerator is a self-hosted API that turns technical CHANGELOG content into ready-to-use release notes for multiple channels (Discord, email, website, app stores) and languages (e.g. English, French). It uses an Ollama instance (OpenAI-compatible API) so you keep full control over the model and data.

Typical use: call the API from your CI/CD with the project’s CHANGELOG, get one generated note per format/language, then copy the result into your artifact or summary and send it manually when you’re satisfied.

## Features

- **Changelog parsing**: Expects the usual `## [vx.x.x] - yyyy-mm-dd` header and `### Added`, `### Changed`, `### Removed`, `### Fixed` sections.
- **Configurable formats**: Each output format (e.g. Discord, email, website, app stores) has an example template in configuration; the model follows that style.
- **Languages**: Generate notes in the languages you configure (e.g. `en`, `fr`).
- **One call per output**: One request = one format and one language, so CI can call the API once per Discord note, once per app store note, etc.
- **No auth**: Designed for controlled environments; no built-in security.

## Requirements

- .NET 10.0 (API)
- Ollama running with a chat model (e.g. `llama3.2`)

## Quick start

1. Start Ollama (e.g. via the repo’s `docker-compose.yml`):

   ```bash
   docker compose up -d ollama
   docker exec -it releasegenerator-ollama ollama pull llama3.2
   ```

2. Configure the API (see [Configuration](#configuration)) and run it:

   ```bash
   cd Api
   dotnet run
   ```

3. Call the generate endpoint (see [API](#api)).

## Configuration

Configuration lives under `ReleaseGenerator` in `appsettings.json` (or environment variables).

| Key | Description |
|-----|-------------|
| `ReleaseGenerator:Ollama:BaseUrl` | Ollama base URL (e.g. `http://localhost:11434` or `http://ollama:11434` in Docker). |
| `ReleaseGenerator:Ollama:Model` | Model name (e.g. `llama3.2`). See [Recommended models](#recommended-ollama-models) for better translation and formatting. |
| `ReleaseGenerator:Ollama:TimeoutSeconds` | Request timeout for Ollama. |
| `ReleaseGenerator:Languages` | List of allowed language codes (e.g. `["en", "fr"]`). |
| `ReleaseGenerator:Formats` | Map of format id → `{ "ExampleTemplate": "..." }`. The model uses the example to match style and structure. You can use placeholders like `{version}` and `{date}` in the example; the prompt tells the model to use the real version and date. |

Example (excerpt):

```json
{
  "ReleaseGenerator": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3.2",
      "TimeoutSeconds": 120
    },
    "Languages": ["en", "fr"],
    "Formats": {
      "discord": {
        "ExampleTemplate": "🎉 **Version {version}** is out!\n\nSummary of changes:\n- Added: ...\n- Fixed: ..."
      },
      "appstores": {
        "ExampleTemplate": "What's New in {version}:\n• ...\n• ..."
      }
    }
  }
}
```

When running the API in Docker, set the Ollama URL via environment, e.g.:

- `ReleaseGenerator__Ollama__BaseUrl=http://ollama:11434`

### Recommended Ollama models

For better reformulation, translation, and strict template formatting (e.g. Discord ** and __), use a larger or more capable model. Examples (pull with `ollama pull <name>`):

| Model | Command | Notes |
|-------|---------|--------|
| **Llama 3.1 70B** | `ollama pull llama3.1:70b` | Strong reasoning and multilingual; higher RAM/VRAM. |
| **Mistral Large** | `ollama pull mistral-large` | Very good multilingual (FR/EN and others), strong instruction following. |
| **Mistral Large 3** | `ollama pull mistral-large-3` | Newer flagship, 256k context, good for translation and formatting. |
| **Mixtral 8x7B** | `ollama pull mixtral` | Good balance of quality and size; solid French/English. |
| **Llama 3.2** | `ollama pull llama3.2` | Default in config; lighter, may drop formatting or translate roughly. |

Set `ReleaseGenerator:Ollama:Model` to the chosen model name (e.g. `mistral-large` or `llama3.1:70b`).

## Changelog format

The API expects a markdown changelog in this shape:

```markdown
## [v1.2.3] - 2025-03-17

### Added

- New feature A;
- New feature B;

### Changed

- Change X;

### Fixed

- Fix Y;
```

- First line: `## [v<version>] - yyyy-mm-dd` (version and date are parsed).
- Sections: `### Added`, `### Changed`, `### Removed`, `### Fixed` (each optional). Items are lines starting with `- `.

If the body is empty or the version line is missing, the API returns an error.

## API

Base path: `/api/ReleaseNotes`.

### Get allowed formats

```http
GET /api/ReleaseNotes/formats
```

Returns a JSON object with a `formats` array (e.g. `["discord", "email", "website", "appstores"]`).

### Get allowed languages

```http
GET /api/ReleaseNotes/languages
```

Returns a JSON object with a `languages` array (e.g. `["en", "fr"]`).

### Generate release note (JSON body)

```http
POST /api/ReleaseNotes/generate
Content-Type: application/json

{
  "changelog": "## [v1.0.0] - 2025-03-17\n\n### Added\n\n- Feature;\n\n### Fixed\n\n- Bug;",
  "format": "discord",
  "language": "en"
}
```

- **changelog** (required): Full changelog text.
- **format** (required): One of the configured format ids (e.g. `discord`, `appstores`).
- **language** (required): One of the configured language codes (e.g. `en`, `fr`).

Response:

- **Plain text** (default): The generated release note as the raw response body, so it displays cleanly in the browser or when piping `curl`.
- **JSON**: Send `Accept: application/json` to get `{ "content": "<generated release note>" }`.

Validation errors (unknown format/language, invalid or empty changelog) return `400` with a JSON body like `{ "error": "..." }`.

### Generate release note (raw changelog body)

For CI, you can send the changelog file as the request body and pass format and language in the query string:

```http
POST /api/ReleaseNotes/generate-from-body?format=discord&language=en
Content-Type: text/plain

<raw CHANGELOG.md content>
```

Example with curl:

```bash
curl -X POST "http://localhost:5041/api/ReleaseNotes/generate-from-body?format=discord&language=en" \
  -H "Content-Type: text/plain" \
  -d @CHANGELOG.md
```

Response is the same as for `POST /generate` (plain text by default, or JSON if `Accept: application/json`).

## CI/CD usage

1. In your pipeline, after the changelog is updated, call the API once per desired output (e.g. once for Discord EN, once for Discord FR, once for app stores).
2. Put each response into an artifact or a job summary so you can copy/paste into Discord, app store, etc.
3. Keep manual control: you decide when to publish; the API only generates the text.

Example (GitHub Actions style):

```yaml
- name: Generate Discord release note
  id: discord
  run: |
    NOTE=$(curl -s -X POST "${{ secrets.RELEASEGENERATOR_URL }}/api/ReleaseNotes/generate-from-body?format=discord&language=en" \
      -H "Content-Type: text/plain" \
      -d @CHANGELOG.md)
    echo "note<<EOF" >> $GITHUB_OUTPUT
    echo "$NOTE" >> $GITHUB_OUTPUT
    echo "EOF" >> $GITHUB_OUTPUT
```

## Docker image CI/CD

A GitHub Actions workflow (`.github/workflows/docker-build-push.yml`) builds the API Docker image and pushes it to your registry on every push to `main` (or on manual trigger).

Configure in the repository **Settings → Secrets and variables → Actions**:

| Type   | Name               | Description                                                                 |
|--------|--------------------|-----------------------------------------------------------------------------|
| Variable | `REGISTRY`         | Full registry path for the image (e.g. `registry.domain.net` or `ghcr.io/owner`). The image will be tagged as `REGISTRY/releasegenerator/api:latest`. |
| Secret | `REGISTRY_USERNAME` | Username to log in to the registry.                                        |
| Secret | `REGISTRY_PASSWORD` | Password or token for the registry.                                         |

The workflow uses the Api Dockerfile and pushes the image as `releasegenerator/api:latest` under your registry.

## Deployment

The `Api` project is intended to run in Docker and be wired in the same `docker-compose` as Ollama. Uncomment and adjust the `releasegenerator` service in `docker-compose.yml`, set `ReleaseGenerator__Ollama__BaseUrl=http://ollama:11434`, and use the image built by CI (e.g. `$REGISTRY/releasegenerator/api:latest`) or build the image locally as needed.

## License

See repository LICENSE file.
