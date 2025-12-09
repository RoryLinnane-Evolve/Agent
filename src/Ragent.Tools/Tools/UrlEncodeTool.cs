using System.Net;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "url_encode", Name = "URL:Encode", Description = "URL-encodes a string using UTF-8.")]
public static class UrlEncodeTool
{
    [ToolLogic]
    public static string Encode([ToolParam(Description = "The text to URL-encode")] string input)
    {
        input ??= string.Empty;
        return WebUtility.UrlEncode(input);
    }
}
