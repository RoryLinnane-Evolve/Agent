using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "str_substring", Name = "String:Substring", Description = "Returns a substring from the specified index and length.")]
public static class StringSubstringTool
{
    [ToolLogic]
    public static string Substring(
        [ToolParam(Description = "The input string")] string input,
        [ToolParam(Description = "The starting index (0-based)")] int startIndex,
        [ToolParam(Description = "The length of the substring")] int length)
    {
        input ??= string.Empty;
        if (startIndex < 0) startIndex = 0;
        if (length < 0) length = 0;
        if (startIndex > input.Length) return string.Empty;
        if (startIndex + length > input.Length) length = input.Length - startIndex;
        return input.Substring(startIndex, length);
    }
}
