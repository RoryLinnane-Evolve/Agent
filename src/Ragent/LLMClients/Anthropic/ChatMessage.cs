using Newtonsoft.Json;

namespace Ragent.LLMClients.Anthropic;

public sealed class ChatMessage
{
    [JsonProperty("role")]
    public required string Role { get; set; }
    [JsonProperty("content")]
    public required string Content { get; set; }
}
