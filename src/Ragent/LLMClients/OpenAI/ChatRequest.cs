using Newtonsoft.Json;

namespace Ragent.LLMClients.OpenAI;

public sealed class ChatRequest
{
    [JsonProperty("model")]
    public required string Model { get; set; }
    [JsonProperty("messages")]
    public required List<ChatMessage> Messages { get; set; }
}
