# Ragent Studio

Ragent Studio is a full-stack sample for exploring the Ragent framework from a browser. It pairs a Svelte chat workspace with an ASP.NET Core controller API and keeps framework integration out of the HTTP and UI layers.

## Architecture

```text
Svelte UI → AgentStudio.Api (controllers) → AgentStudio.Application (contracts) → AgentStudio.Infrastructure (Ragent runtime) → Ragent
                                             ↑
                                      AgentStudio.Domain (conversation model)
```

| Project | Responsibility |
| --- | --- |
| `backend/AgentStudio.Domain` | Framework-independent conversation records. |
| `backend/AgentStudio.Application` | DTOs and the `IAgentWorkspace` application boundary. |
| `backend/AgentStudio.Infrastructure` | Per-conversation Ragent instances, synchronization, model configuration, and tool mapping. |
| `backend/AgentStudio.Api` | ASP.NET Core composition root, error handling, static UI hosting, and `AgentWorkspaceController`. |
| `frontend` | Svelte/Vite single-page UI, built into the API's `wwwroot` directory. |

The API keeps a separate mutable `Agent` instance and async lock for each browser conversation. This is important because `Agent` owns chat history and is not thread-safe.

## Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) (the repository currently pins the verified preview SDK in `global.json`)
- Node.js 22.12+ (or newer) and npm
- [Ollama](https://ollama.com/) with an installed model when using the default runtime:

  ```bash
  ollama pull mistral
  ollama serve
  ```

## Run locally

From the repository root:

```bash
dotnet restore Agent.sln
npm install --prefix sample/web/frontend
npm run build --prefix sample/web/frontend
dotnet run --project sample/web/backend/AgentStudio.Api --urls http://localhost:3000
```

Open `http://localhost:3000`. The controller API is available under `/api/agent`.

The default model is `OLLAMA_MISTRAL`, configured in `backend/AgentStudio.Api/appsettings.json`. Set `AgentRuntime__Model` to any supported `EModel` value before starting the API, for example:

```bash
AgentRuntime__Model=OLLAMA_LLAMA32 dotnet run --project sample/web/backend/AgentStudio.Api
```

For Gemini, use `AgentRuntime__Model=GEMINI_2_5_FLASH` and configure credentials required by the Google GenAI SDK in your environment. Do not place secrets in `appsettings.json`.

## Validate

```bash
dotnet build Agent.sln
dotnet test Agent.sln --no-build
npm run check --prefix sample/web/frontend
```

## Known limitations

- The framework's current provider clients do not expose streaming or cancellation, so the UI waits for a complete response and shows a sending state. The sample returns an error after 15 seconds if the configured provider has not responded.
- Conversations are in-memory and reset when the API restarts.
- A usable provider is required for sending messages. Without an Ollama server/model or Gemini credentials, the UI shows a safe provider error rather than fabricating a response or exposing provider exception details.
