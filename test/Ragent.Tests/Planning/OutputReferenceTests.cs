using Ragent.Agent.Planning;

namespace Ragent.Tests.Planning;

public class OutputReferenceTests
{
    [Fact]
    public void FindReferences_NoPlaceholders_ReturnsEmpty()
    {
        Assert.Empty(OutputReference.FindReferences("plain value"));
    }

    [Fact]
    public void FindReferences_FindsDistinctStepIds()
    {
        var refs = OutputReference.FindReferences("{{s1}} and {{s2}} and {{s1}}");
        Assert.Equal(["s1", "s2"], refs);
    }

    [Fact]
    public void FindReferences_ToleratesWhitespaceInsidePlaceholder()
    {
        Assert.Equal(["s1"], OutputReference.FindReferences("{{ s1 }}"));
    }

    [Fact]
    public void Substitute_ReplacesKnownReferences()
    {
        var outputs = new Dictionary<string, string> { ["s1"] = "hello", ["s2"] = "world" };

        var result = OutputReference.Substitute("{{s1}}, {{s2}}!", outputs);

        Assert.Equal("hello, world!", result);
    }

    [Fact]
    public void Substitute_LeavesUnknownReferencesUntouched()
    {
        var outputs = new Dictionary<string, string> { ["s1"] = "hello" };

        var result = OutputReference.Substitute("{{s1}} {{missing}}", outputs);

        Assert.Equal("hello {{missing}}", result);
    }
}
