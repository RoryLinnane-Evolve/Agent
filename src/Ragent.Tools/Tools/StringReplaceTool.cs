using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "str_replace", Name = "String:Replace", Description = "Replaces all occurrences of a substring with another substring.")]
public static class StringReplaceTool
{
    [ToolLogic]
    public static string Replace(
        [ToolParam(Description = "The input string")] string input,
        [ToolParam(Description = "The substring to find")] string oldValue,
        [ToolParam(Description = "The replacement substring")] string newValue)
    {
        input ??= string.Empty;
        oldValue ??= string.Empty;
        newValue ??= string.Empty;
        return input.Replace(oldValue, newValue);
    }
}
