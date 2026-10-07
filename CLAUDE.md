# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
```bash
dotnet build Agent.sln
```

### Test
```bash
# Run all tests
dotnet test Agent.sln

# Run a specific test project
dotnet test test/Ragent.Tests/Ragent.Tests.csproj
dotnet test test/Ragent.Tools.Tests/Ragent.Tools.Tests.csproj

# Run a single test by name
dotnet test test/Ragent.Tests/Ragent.Tests.csproj --filter "FullyQualifiedName~AgentDiscoveryTests.Agent_Initializes_To_Idle_And_Loads_Tools"

# Run with coverage
dotnet test Agent.sln --collect:"XPlat Code Coverage"
```

### Run the CLI sample
```bash
dotnet run --project sample/cli/cli.csproj
```

## Architecture

This is a .NET 10 solution (`Agent.sln`) for **Ragent**, a lightweight framework for building tool-augmented AI agents. Two NuGet packages are published from this repo: `Ragent` (core) and `Ragent.Tools` (built-in tools).

### Core flow

1. `Agent` (`src/Ragent/Agent/Agent.cs`) is constructed with an `AgentConfig` specifying the LLM model and tool configuration.
2. On construction, the agent scans assemblies for classes marked `[ToolCollection]` and methods marked `[Tool]` via reflection, building a list of `ToolInfo` and a `Dictionary<string, MethodInfo>`.
3. Tool descriptions are injected into the system prompt (`Prompts/tool_picker_prompt.md`, embedded resource), replacing the `{tools}` placeholder.
4. When `ProcessMessage(string)` is called, the LLM either returns plain text (direct response) or JSON matching the `ToolCall` schema (`{ "toolId": "...", "params": [...] }`). The agent deserializes and routes accordingly.

### Key abstractions

- **`ILLMClient`** (`src/Ragent/LLMClients/ILLMClient.cs`): single method `Task<string> Send(string message)`. Implementations: `OllamaClient`, `GeminiClient`, `OpenAIClient`, `AnthropicClient`. Selected via `EModel` enum in `AgentConfig`.
- **`[ToolCollection]`** / **`[Tool]`** / **`[ToolParam]`**: Attributes in `Ragent.Reflection` used to mark static methods for automatic tool discovery.
- **`AgentConfig`** (`src/Ragent/Config/Config.cs`): configures the model, tool blacklist, system prompt overrides, retry/history limits, and additional assemblies to scan.
- **`Message`**: a record struct with `EMessageType` (USER, AGENT, TOOL_RESULT, TOOL_ERROR, AGENT_ERROR) and `Content`.

### Tool authoring pattern

Tools must be `public static` methods on a class decorated with `[ToolCollection]`. The containing assembly must be registered via `AgentConfig.AdditionalAssemblies` if it's not the entry assembly or Ragent core.

```csharp
[ToolCollection]
public class MyTools {
    [Tool(Id = "my_tool", Name = "My Tool", Description = "Does something")]
    public static string DoSomething([ToolParam(Description = "Input value")] string input) {
        return "result";
    }
}
```

### Adding a new LLM backend

Add an `EModel` variant, implement `ILLMClient`, and add a case to the `CreateClient` switch in `Agent.cs`.

### Project layout

- `src/Ragent/` — core framework (NuGet: `Ragent`)
- `src/Ragent.Tools/` — optional built-in tools (NuGet: `Ragent.Tools`); register via `AdditionalAssemblies = [typeof(RagentTools).Assembly]`
- `test/Ragent.Tests/` — xUnit tests for core (uses `NullLogger`, `DummyCalculatorTool` fixture)
- `test/Ragent.Tools.Tests/` — xUnit tests for built-in tools
- `sample/cli/` — Terminal.Gui CLI demo wiring up the agent with a TUI