using System.Text.Json.Serialization;

namespace Ragent.LLMClients.Ollama;

public sealed class ChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }
    [JsonPropertyName("messages")]
    public required ChatMessage[] messages { get; set; }
}