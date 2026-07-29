using Newtonsoft.Json;

namespace Ragent.LLMClients.Anthropic;

public sealed class ChatResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("role")]
    public required string Role { get; set; }

    [JsonProperty("content")]
    public required List<ContentBlock> Content { get; set; }

    [JsonProperty("stop_reason")]
    public string? StopReason { get; set; }

    [JsonProperty("usage")]
    public Usage? Usage { get; set; }
}

public sealed class ContentBlock
{
    [JsonProperty("type")]
    public required string Type { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }
}

public sealed class Usage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
}
