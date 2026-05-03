# Ragent.Tools

Built-in tool collections for the [Ragent](https://www.nuget.org/packages/Ragent) agentic framework.

## Installation

```
dotnet add package Ragent.Tools
```

## Usage

Register the `Ragent.Tools` assembly in your `AgentConfig` so the agent can discover and call the built-in tools:

```csharp
using Ragent.Config;
using Ragent.LLMClients;
using Ragent.Tools;

var config = new AgentConfig {
    Model = EModel.GEMINI_2_5_FLASH,
    AdditionalAssemblies = [typeof(RagentTools).Assembly]
};
```

`RagentTools` is a marker class — referencing its assembly is all that is needed for Ragent to scan and load every tool defined in this package.

## Included Tools

### Database (`Ragent.Tools.DB`)

| Tool ID | Name | Description |
|---|---|---|
| `query_db` | Query the database | Executes a SQL query and returns results |

> **Note:** The DB tool is a stub. Connect it to your data source by extending the `Query` class or replacing it with your own `[ToolCollection]`.

## Adding Your Own Tools

You do not need this package to define custom tools. Any `[ToolCollection]` class in your entry assembly is discovered automatically:

```csharp
using Ragent.Reflection;

[ToolCollection]
public static class WeatherTools
{
    [Tool(Id = "get_weather", Name = "Get Weather", Description = "Returns current weather for a city")]
    public static string GetWeather(
        [ToolParam(Description = "City name")] string city)
        => $"It is sunny in {city}.";
}
```

No registration required — Ragent scans your entry assembly automatically.

## Source & Issues

[github.com/RoryLinnane-Evolve/Agent](https://github.com/RoryLinnane-Evolve/Agent)
