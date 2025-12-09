using System.Net;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "url_decode", Name = "URL:Decode", Description = "URL-decodes a string using UTF-8.")]
public static class UrlDecodeTool
{
    [ToolLogic]
    public static string Decode([ToolParam(Description = "The text to URL-decode")] string input)
    {
        input ??= string.Empty;
        return WebUtility.UrlDecode(input);
    }
}
