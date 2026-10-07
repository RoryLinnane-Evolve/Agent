namespace AgentStudio.Infrastructure;

public sealed class AgentRuntimeOptions
{
    public const string SectionName = "AgentRuntime";
    public string Model { get; set; } = "OLLAMA_MISTRAL";
}
