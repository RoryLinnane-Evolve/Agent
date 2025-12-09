using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "json_pretty", Name = "JSON:Pretty", Description = "Pretty-prints a compact JSON string with indentation. Returns input if parsing fails.")]
public static class JsonPrettyTool
{
    [ToolLogic]
    public static string Pretty(
        [ToolParam(Description = "A JSON string to pretty-print")] string json)
    {
        json ??= string.Empty;
        try
        {
            var token = JToken.Parse(json);
            return token.ToString(Formatting.Indented);
        }
        catch
        {
            return json; // return original if not valid JSON
        }
    }
}
