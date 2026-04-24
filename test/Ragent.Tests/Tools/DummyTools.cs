using Ragent.Reflection;

namespace Ragent.Tests.Tools;

[ToolCollection]
public static class DummyCalculatorTool
{
    [Tool(Id = "calc_add", Name = "Calculator:Add", Description = "A dummy calculator tool for tests.")]
    public static int Add(
        [ToolParam(Description = "Left operand")] int a,
        [ToolParam(Description = "Right operand")] int b)
    {
        return a + b;
    }
}

[ToolCollection]
public static class DummyFailingTool
{
    public static int CallCount { get; private set; }

    public static void ResetCallCount() => CallCount = 0;

    [Tool(Id = "always_fails", Name = "Always Fails", Description = "A tool that always throws, used to test retry logic.")]
    public static string AlwaysFails([ToolParam(Description = "Input value")] string input)
    {
        CallCount++;
        throw new InvalidOperationException("This tool always fails");
    }
}
