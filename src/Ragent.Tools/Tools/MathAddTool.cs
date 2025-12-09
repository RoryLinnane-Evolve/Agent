using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "math_add", Name = "Math:Add", Description = "Adds two numbers and returns the sum.")]
public static class MathAddTool
{
    [ToolLogic]
    public static double Add(
        [ToolParam(Description = "First addend")] double a,
        [ToolParam(Description = "Second addend")] double b)
        => a + b;
}
