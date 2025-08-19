using System.Text.Json.Serialization;

namespace Ragent.Chat;

public class ChatResponse
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("created_at")]
    public required DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public required ChatMessage Message { get; set; }

    [JsonPropertyName("done")]
    public required bool Done { get; set; }

    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; set; }

    [JsonPropertyName("total_duration")]
    public long? TotalDuration { get; set; }

    [JsonPropertyName("load_duration")]
    public long? LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long? PromptEvalDuration { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get;set; }

    [JsonPropertyName("eval_duration")]
    public long? EvalDuration { get; set; }
}