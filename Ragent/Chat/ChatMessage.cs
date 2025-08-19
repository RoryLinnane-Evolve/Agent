using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Ragent.Chat;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}