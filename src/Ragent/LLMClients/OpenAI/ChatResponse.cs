using Newtonsoft.Json;

namespace Ragent.LLMClients.OpenAI;

public sealed class ChatResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("choices")]
    public required List<Choice> Choices { get; set; }

    [JsonProperty("usage")]
    public Usage? Usage { get; set; }
}

public sealed class Choice
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public required ChatMessage Message { get; set; }

    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class Usage
{
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}
