using Ragent;

namespace Ragent.Config;

public sealed class AgentConfig {
    /// <summary>
    /// The LLM backend to use.
    /// </summary>
    public required EModel Model { get; set; }

    /// <summary>
    /// Maximum number of tool-call iterations per ProcessMessage call before the agent stops.
    /// </summary>
    public int MaxIterations { get; set; } = 5;

    /// <summary>
    /// How many times to retry a failed tool call before giving up.
    /// </summary>
    public ushort MaxToolRetries { get; set; } = 1;

    /// <summary>
    /// Appended to the end of the loaded system prompt. Use this to add context without replacing the default prompt.
    /// </summary>
    public string? ExtraSystemInstructions { get; set; }

    /// <summary>
    /// If set, replaces the embedded system prompt entirely.
    /// </summary>
    public string? SystemPromptOverride { get; set; }

    /// <summary>
    /// Maximum number of messages retained in chat history. Oldest messages are dropped when the limit is exceeded. Null means unlimited.
    /// </summary>
    public int? MaxChatHistorySize { get; set; }
    
    /// <summary>
    /// Tool IDs that should not be exposed to the LLM. All other discovered tools are available.
    /// </summary>
    public List<string> ToolIdsBlackList { get; set; } = [];
}