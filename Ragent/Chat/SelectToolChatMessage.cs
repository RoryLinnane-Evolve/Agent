using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Ragent.Chat;

public class SelectToolChatMessage : ChatMessage
{
    [JsonPropertyName("toolName")]
    public required string ToolName { get; set; }
    [JsonPropertyName("parameters")]
    public dynamic[]? Parameters { get; set; }
}