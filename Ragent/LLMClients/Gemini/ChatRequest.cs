
using System.Text.Json.Serialization;

namespace Ragent.LLMClients.Gemini;

public sealed class ChatRequest {
    [JsonPropertyName("Contents")]
    public required List<ChatMessage> Contents { get; set; }
}

public sealed class ChatMessage {
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    [JsonPropertyName("parts")]
    public required List<Dictionary<string,string>> Parts { get; set; }
}