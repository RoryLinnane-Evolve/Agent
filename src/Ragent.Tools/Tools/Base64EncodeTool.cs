using System.Text;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "b64_encode", Name = "Base64:Encode", Description = "Encodes a UTF-8 string to Base64.")]
public static class Base64EncodeTool
{
    [ToolLogic]
    public static string Encode([ToolParam(Description = "Input text to encode as Base64")] string input)
    {
        input ??= string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }
}
