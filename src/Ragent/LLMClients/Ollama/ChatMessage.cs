using System.Text.Json.Serialization;

namespace Ragent.LLMClients.Ollama;

public sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}