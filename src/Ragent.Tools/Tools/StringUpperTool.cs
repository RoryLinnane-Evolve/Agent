using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "str_upper", Name = "String:ToUpper", Description = "Converts a string to upper case using invariant culture.")]
public static class StringUpperTool
{
    [ToolLogic]
    public static string ToUpper([ToolParam(Description = "The input string to convert to upper case")] string input)
        => (input ?? string.Empty).ToUpperInvariant();
}
