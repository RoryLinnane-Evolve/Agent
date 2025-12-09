using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "math_multiply", Name = "Math:Multiply", Description = "Multiplies two numbers and returns the product.")]
public static class MathMultiplyTool
{
    [ToolLogic]
    public static double Multiply(
        [ToolParam(Description = "First factor")] double a,
        [ToolParam(Description = "Second factor")] double b)
        => a * b;
}
