using Ragent.Reflection;

namespace Ragent.Tests.Tools;

[Tool(Description = "A dummy calculator tool for tests.", Id = "calc_add", Name = "Calculator:Add")]
public static class DummyCalculatorTool
{
    [ToolLogic]
    public static int Add(
        [ToolParam(Description = "Left operand")] int a,
        [ToolParam(Description = "Right operand")] int b)
    {
        return a + b;
    }
}
