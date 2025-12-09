using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "text_truncate", Name = "Text:Truncate", Description = "Truncates text to a maximum length, optionally adding an ellipsis (…)).")]
public static class TextTruncateTool
{
    [ToolLogic]
    public static string Truncate(
        [ToolParam(Description = "The input text")] string input,
        [ToolParam(Description = "The maximum length to keep")] int maxLength,
        [ToolParam(Description = "If true, appends an ellipsis when truncated")] bool ellipsis)
    {
        input ??= string.Empty;
        if (maxLength < 0) return string.Empty;
        if (input.Length <= maxLength) return input;
        if (!ellipsis || maxLength <= 1) return input.Substring(0, maxLength);
        var cut = maxLength - 1;
        return input.Substring(0, cut) + "…";
    }
}
