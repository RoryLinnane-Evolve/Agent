using System.Security.Cryptography;
using System.Text;
using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "hash_sha256", Name = "Hash:SHA256", Description = "Computes the SHA-256 hash of the input text and returns a hex string.")]
public static class Sha256HashTool
{
    [ToolLogic]
    public static string Sha256(
        [ToolParam(Description = "The input text to hash (UTF-8)")] string input)
    {
        input ??= string.Empty;
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
