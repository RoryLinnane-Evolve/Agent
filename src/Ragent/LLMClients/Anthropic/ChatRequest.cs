using Newtonsoft.Json;

namespace Ragent.LLMClients.Anthropic;

public sealed class ChatRequest
{
    [JsonProperty("model")]
    public required string Model { get; set; }

    /// <summary>
    /// The Anthropic messages API takes the system prompt as a top-level field
    /// rather than a message with a "system" role.
    /// </summary>
    [JsonProperty("system")]
    public required string System { get; set; }

    [JsonProperty("messages")]
    public required List<ChatMessage> Messages { get; set; }

    /// <summary>
    /// Required by the Anthropic messages API.
    /// </summary>
    [JsonProperty("max_tokens")]
    public required int MaxTokens { get; set; }
}
