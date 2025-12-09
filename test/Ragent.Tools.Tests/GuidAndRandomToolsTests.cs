using System;
using Ragent.Tools.Tools;

namespace Ragent.Tools.Tests;

public class GuidAndRandomToolsTests
{
    [Fact]
    public void GuidGenerate_Returns_Valid_Guid()
    {
        var s = GuidGenerateTool.NewGuid();
        Assert.True(Guid.TryParse(s, out var g));
        Assert.NotEqual(Guid.Empty, g);
    }

    [Theory]
    [InlineData(0, 1, 1234)]
    [InlineData(-10, -5, 42)]
    public void RandomInt_With_Seed_Is_Deterministic(int min, int max, int seed)
    {
        var r1 = RandomIntTool.Next(min, max, seed);
        var r2 = RandomIntTool.Next(min, max, seed);
        Assert.Equal(r1, r2);
        Assert.InRange(r1, min, max - 1);
    }

    [Fact]
    public void RandomInt_MaxLessOrEqualMin_Returns_Min()
    {
        Assert.Equal(5, RandomIntTool.Next(5, 5, 0));
        Assert.Equal(7, RandomIntTool.Next(7, 3, 0));
    }
}
