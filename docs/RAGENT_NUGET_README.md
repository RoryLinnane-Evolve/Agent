# Ragent

A lightweight .NET agentic framework with tool discovery, LLM abstraction, and configuration.

## Installation

```
dotnet add package Ragent
```

## Quick Start

```csharp
using Microsoft.Extensions.Logging;
using Ragent.Agent;
using Ragent.Config;
using Ragent.LLMClients;

var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger<Agent>();

var agent = new Agent(logger, new AgentConfig {
    Model = EModel.GEMINI_2_5_FLASH
});

agent.OnMessageReceived = async () => {
    Console.WriteLine(agent.ChatHistory.Last().Content);
    await Task.CompletedTask;
};

await agent.ProcessMessage("What is the square root of 144?");
```

## Configuration

`AgentConfig` controls all agent behaviour:

```csharp
var config = new AgentConfig {
    Model                  = EModel.GEMINI_2_5_FLASH,
    MaxIterations          = 5,
    MaxParallelTools       = 4,
    MaxToolRetries         = 2,
    MaxChatHistorySize     = 50,
    ExtraSystemInstructions = "Always respond concisely.",
    SystemPromptOverride   = null,           // replaces the built-in prompt entirely
    ToolIdsBlackList       = ["query_db"],   // tool IDs to hide from the LLM
    AdditionalAssemblies   = [typeof(RagentTools).Assembly]  // third-party tool packages
};
```

| Property | Default | Description |
|---|---|---|
| `Model` | required | LLM backend to use |
| `LLMClientFactory` | `null` | Custom LLM client factory; takes precedence over `Model` |
| `MaxIterations` | `5` | Max plan-execute iterations per message |
| `MaxParallelTools` | `4` | Max independent plan steps running concurrently |
| `MaxToolRetries` | `1` | Retries on tool failure |
| `MaxChatHistorySize` | `null` (unlimited) | Oldest messages dropped when exceeded |
| `ExtraSystemInstructions` | `null` | Appended to the built-in system prompt |
| `SystemPromptOverride` | `null` | Replaces the built-in system prompt entirely |
| `ToolIdsBlackList` | `[]` | Tool IDs hidden from the LLM |
| `AdditionalAssemblies` | `[]` | Extra assemblies scanned for tools |

## Workflow Plans

When a request needs tools, the LLM replies with a deterministic JSON plan. Each step calls one tool;
`{{stepId}}` placeholders map one step's output onto another step's input:

```json
{ "plan": [
  { "stepId": "s1", "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://example.com/a" } ] },
  { "stepId": "s2", "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://example.com/b" } ] },
  { "stepId": "s3", "toolId": "summarise", "params": [ { "name": "text", "value": "{{s1}}\n{{s2}}" } ] }
] }
```

- Steps with no dependency between them run **in parallel** (bounded by `MaxParallelTools`).
- Plans are validated before execution: unknown tools, unknown step references, duplicate step IDs,
  and dependency cycles are rejected and the LLM is asked to correct the plan.
- If a step fails, steps that depend on it are skipped with a clear error; independent steps still run.
- After execution the LLM sees all step results and may reply with a follow-up plan or a final
  plain-text answer, up to `MaxIterations`.

## Supported Models

| Enum | Backend | Model |
|---|---|---|
| `EModel.GEMINI_2_5_FLASH` | Google Gemini | gemini-2.5-flash |
| `EModel.OPENAI_GPT_4O` | OpenAI | gpt-4o |
| `EModel.OPENAI_GPT_4O_MINI` | OpenAI | gpt-4o-mini |
| `EModel.ANTHROPIC_CLAUDE_SONNET_4_5` | Anthropic | claude-sonnet-4-5 |
| `EModel.ANTHROPIC_CLAUDE_HAIKU_4_5` | Anthropic | claude-haiku-4-5 |
| `EModel.OLLAMA_MISTRAL` | Ollama (local) | mistral |
| `EModel.OLLAMA_LLAMA32` | Ollama (local) | llama3.2 |

### Provider Credentials

| Backend | Requirement |
|---|---|
| Google Gemini | `GOOGLE_API_KEY` / `GEMINI_API_KEY` environment variable (resolved by the Google GenAI SDK) |
| OpenAI | `OPENAI_API_KEY` environment variable |
| Anthropic | `ANTHROPIC_API_KEY` environment variable |
| Ollama | A local Ollama server on `http://localhost:11434` |

## Defining Tools

Tools are discovered automatically from the entry assembly. Decorate a static class and its methods:

```csharp
using Ragent.Reflection;

[ToolCollection]
public static class MathTools
{
    [Tool(Id = "sqrt", Name = "Square Root", Description = "Returns the square root of a number")]
    public static double SquareRoot(
        [ToolParam(Description = "The input number")] double value)
        => Math.Sqrt(value);
}
```

- `[ToolCollection]` marks a class as a source of tools.
- `[Tool]` marks a public static method as an invocable tool.
- `[ToolParam]` annotates parameters with descriptions sent to the LLM.
- Tools may be synchronous or return `Task`/`Task<T>`; async tools are awaited.

## Tool Discovery

Ragent scans three sources for tools automatically:

1. **Ragent core assembly** — built-in tools.
2. **Entry assembly** — your application's tools, discovered automatically with no configuration.
3. **`AdditionalAssemblies`** — third-party tool packages (e.g. `Ragent.Tools`).

```csharp
// Load built-in Ragent.Tools package alongside your own tools
AdditionalAssemblies = [typeof(RagentTools).Assembly]
```

## Dependency Injection (Blazor / ASP.NET Core)

```csharp
builder.Services.AddSingleton(new AgentConfig { Model = EModel.GEMINI_2_5_FLASH });
builder.Services.AddScoped<Agent>();
```

## Events

```csharp
// Func<Task> — awaited by the agent after every status change
agent.OnMessageReceived = () => InvokeAsync(StateHasChanged);
```

## Source & Issues

[github.com/RoryLinnane-Evolve/Agent](https://github.com/RoryLinnane-Evolve/Agent)
