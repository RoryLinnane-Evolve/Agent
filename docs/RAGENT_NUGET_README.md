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
| `MaxIterations` | `5` | Max tool-call loops per message |
| `MaxToolRetries` | `1` | Retries on tool failure |
| `MaxChatHistorySize` | `null` (unlimited) | Oldest messages dropped when exceeded |
| `ExtraSystemInstructions` | `null` | Appended to the built-in system prompt |
| `SystemPromptOverride` | `null` | Replaces the built-in system prompt entirely |
| `ToolIdsBlackList` | `[]` | Tool IDs hidden from the LLM |
| `AdditionalAssemblies` | `[]` | Extra assemblies scanned for tools |

## Supported Models

| Enum | Backend | Model |
|---|---|---|
| `EModel.GEMINI_2_5_FLASH` | Google Gemini | gemini-2.5-flash |
| `EModel.OLLAMA_MISTRAL` | Ollama (local) | mistral |
| `EModel.OLLAMA_LLAMA32` | Ollama (local) | llama3.2 |

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
