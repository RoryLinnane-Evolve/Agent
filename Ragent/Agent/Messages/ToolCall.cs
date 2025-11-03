using Newtonsoft.Json;

namespace Ragent.Agent.Messages;

class ToolCall {
    [JsonProperty(PropertyName = "toolId")]
    public required string Id { get; set; }
    [JsonProperty(PropertyName = "params")]
    public required List<ParamPair> Params { get; set; }
}