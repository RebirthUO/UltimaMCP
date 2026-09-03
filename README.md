# UltimaMCP

UltimaMCP is a localhost-only .NET 10 bridge over the Ultima SDK. It exposes classic Ultima Online client data as semantic JSON through REST/OpenAPI and as model-oriented tools through MCP Streamable HTTP.

The required `Ultima.dll` is included at the repository root and copied to the application output during builds. UltimaMCP validates inputs and serializes access around the SDK because it uses mutable static caches and shared read buffers.

## Requirements

- Windows and .NET 10 SDK
- A legal local Ultima Online Classic installation

Image rendering uses `System.Drawing` and is Windows-only. The server binds to `127.0.0.1:5027` by default and has no authentication; do not expose it on an external interface without adding authentication and transport security.

## Configure the client files

Set `Ultima:DataPath` in `UltimaAPI/appsettings.json`:

```json
{
  "Ultima": {
	"DataPath": "C:\\Games\\Ultima Online Classic"
  }
}
```

Alternatively, leave the value `null` to use the Windows registry discovery implemented by the SDK, or set an environment variable:

```powershell
$env:Ultima__DataPath = 'C:\Games\Ultima Online Classic'
dotnet run --project .\UltimaAPI\UltimaAPI.csproj
```

Startup succeeds in degraded mode when the directory or optional expansion files are absent. Inspect availability at:

- `GET http://127.0.0.1:5027/api/sdk/health`
- `GET http://127.0.0.1:5027/api/sdk/diagnostics`
- `GET http://127.0.0.1:5027/api/sdk/files`

### UOP compatibility cache

The legacy SDK opens UOP containers with read/write access even for read operations. Standard installations under `Program Files` are read-only, so UltimaMCP creates a writable shadow copy on first use under `%LOCALAPPDATA%\UltimaMCP\UopCache`. Copies are reused until the source file size or modification time changes.

Set `Ultima:UopCachePath` to move the cache, or set `Ultima:EnableWritableUopShadow` to `false` when the client directory is already writable. The original game files are never modified.

## MCP connection

The Streamable HTTP endpoint is:

```text
http://127.0.0.1:5027/mcp
```

A typical MCP client configuration is:

```json
{
  "servers": {
	"ultima": {
	  "type": "http",
	  "url": "http://127.0.0.1:5027/mcp"
	}
  }
}
```

Configuration file names differ between Visual Studio, VS Code, LM Studio, Ollama integrations, and other MCP hosts. Use the host's Streamable HTTP server configuration and the URL above.

MCP tools return compact semantic records. PNG and WAV content is exposed through REST URLs in those records rather than embedded as base64 in model context.

## REST and OpenAPI

In Development, OpenAPI is available at `/openapi/v1.json` and Scalar at `/scalar`.

Major route groups:

| Domain | Examples |
| --- | --- |
| Tile data | `/api/tiles/items/{id}`, `/api/tiles/items/search`, `/api/tiles/land/{id}` |
| Maps | `/api/maps`, `/api/maps/{mapId}/coordinates/{x}/{y}`, `/api/maps/{mapId}/regions/{x}/{y}` |
| Map images | `/api/maps/{mapId}/render.png`, `/api/multimap.png`, `/api/facets/{facetId}.png` |
| Art and UI | `/api/art/land/{id}.png`, `/api/art/static/{id}.png`, `/api/gumps/{id}.png` |
| Textures and lights | `/api/textures/{id}.png`, `/api/lights/{id}.png` |
| Hues and radar | `/api/hues/{id}`, `/api/hues/search`, `/api/hues/radar/{kind}/{id}` |
| Fonts | `/api/fonts`, `/api/fonts/ascii/{fontId}/render.png` |
| Sounds | `/api/sounds/{id}`, `/api/sounds/{id}.wav`, `/api/sounds/search` |
| Animations | `/api/animations/sets`, `/api/animations/{fileType}/{body}/{action}/{direction}` |
| Multis | `/api/multis/{id}`, `/api/multis/{id}.png` |
| Skills | `/api/skills`, `/api/skills/groups` |
| Localized strings | `/api/strings/{language}/{number}`, `/api/strings/{language}/search` |
| Speech and patches | `/api/speech/search`, `/api/verdata` |

Search, region, render, text, multi-component, and animation-frame payloads are bounded by `Ultima:Limits` in `appsettings.json`.

## Read-only coverage

The bridge reads tile data, maps/statics, art, gumps, hues, textures, light masks, radar colors, fonts, sounds, animations, animdata, multis, skills/groups, cliloc strings, speech entries, multi-map/facet images, and verdata patch metadata.

SDK methods that mutate, import, replace, remove, save, or attach to a live client process are intentionally not exposed.

## Build and test

UltimaAPI only: `.\build.ps1` (optional `-Configuration Release`).

```powershell
dotnet build .\UltimaMCP.slnx
dotnet test .\UltimaAPI.Tests\UltimaAPI.Tests.csproj
```

The HTTP tests use a deliberately missing temporary data path, so they validate degraded behavior and request bounds without requiring local game assets.
