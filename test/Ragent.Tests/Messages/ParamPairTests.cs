using Newtonsoft.Json;
using Ragent.Agent.Messages;

namespace Ragent.Tests.Messages;

public class ParamPairTests
{
    [Fact]
    public void Implicit_Tuple_Conversion_Works()
    {
        ParamPair pair = ("foo", "bar");
        (string name, string value) tuple = pair;
        Assert.Equal("foo", tuple.name);
        Assert.Equal("bar", tuple.value);
    }

    [Fact]
    public void Json_Serialization_Property_Names()
    {
        var pair = new ParamPair { Name = "url", Value = "https://example.com" };
        var json = JsonConvert.SerializeObject(pair);
        Assert.Contains("\"name\":\"url\"", json);
        Assert.Contains("\"value\":\"https://example.com\"", json);
    }
}
