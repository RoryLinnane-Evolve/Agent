using Ragent.Tools.Tools;

namespace Ragent.Tools.Tests;

public class MathToolsTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 3)]
    [InlineData(-5.5, 2.5, -3.0)]
    public void MathAdd_Works(double a, double b, double expected)
    {
        var sum = MathAddTool.Add(a, b);
        Assert.Equal(expected, sum, precision: 10);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 2, 6)]
    [InlineData(-3, 2, -6)]
    public void MathMultiply_Works(double a, double b, double expected)
    {
        var prod = MathMultiplyTool.Multiply(a, b);
        Assert.Equal(expected, prod, precision: 10);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("1", 1)]
    [InlineData("1,2,3", 2)]
    [InlineData("1, 2.5, x, 3.5", 2.3333333333)]
    public void MathAverage_Works(string csv, double expected)
    {
        var avg = MathAverageTool.Average(csv);
        Assert.Equal(expected, avg, precision: 6);
    }
}
