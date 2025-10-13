using Newtonsoft.Json;

namespace Ragent.Tools;

public class ParamPair {
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(PropertyName = "value")]
    public string Value { get; set; } = string.Empty;

    // For easy conversion to/from tuples
    public static implicit operator (string, string)(ParamPair pair) => (pair.Name, pair.Value);
    public static implicit operator ParamPair((string name, string value) tuple) => new ParamPair { Name = tuple.name, Value = tuple.value };
}