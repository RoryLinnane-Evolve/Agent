using System.Text;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "b64_decode", Name = "Base64:Decode", Description = "Decodes a Base64 string into UTF-8 text.")]
public static class Base64DecodeTool
{
    [ToolLogic]
    public static string Decode([ToolParam(Description = "Base64 text to decode")] string base64)
    {
        base64 ??= string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "Invalid Base64";
        }
    }
}
