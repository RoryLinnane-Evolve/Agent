using Ragent.Tools.Tools;

namespace Ragent.Tools.Tests;

public class Base64ToolsTests
{
    [Theory]
    [InlineData("hello", "aGVsbG8=")]
    [InlineData("", "")] // empty -> empty
    [InlineData(null, "")] // null treated as empty
    public void Base64Encode_Works(string? input, string expected)
    {
        var actual = Base64EncodeTool.Encode(input!);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("aGVsbG8=", "hello")] // valid
    [InlineData("", "")] // empty -> empty
    public void Base64Decode_Valid(string base64, string expected)
    {
        var actual = Base64DecodeTool.Decode(base64);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("****")] 
    public void Base64Decode_Invalid_Returns_Message(string base64)
    {
        var actual = Base64DecodeTool.Decode(base64);
        Assert.Equal("Invalid Base64", actual);
    }
}
