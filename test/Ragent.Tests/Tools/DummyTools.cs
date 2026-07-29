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

[ToolCollection]
public static class DummyAsyncTool
{
    [Tool(Id = "async_echo", Name = "Async Echo", Description = "An async tool that echoes its input, used to test Task-returning tools.")]
    public static async Task<string> EchoAsync([ToolParam(Description = "Text to echo")] string text)
    {
        await Task.Delay(1);
        return $"echo:{text}";
    }
}

[ToolCollection]
public static class DummyThrowingTool
{
    [Tool(Id = "throws_once_invoked", Name = "Throws", Description = "A tool that always throws, used to test error surfacing without shared state.")]
    public static string Throw([ToolParam(Description = "Input value")] string input)
        => throw new InvalidOperationException("This tool always fails");
}

[ToolCollection]
public static class DummySlowTool
{
    private static int _concurrent;
    private static int _maxConcurrent;

    public static int MaxConcurrent => _maxConcurrent;

    public static void Reset() { _concurrent = 0; _maxConcurrent = 0; }

    [Tool(Id = "slow_echo", Name = "Slow Echo", Description = "A slow tool that records concurrency, used to test parallel execution.")]
    public static string SlowEcho([ToolParam(Description = "Text to echo")] string text)
    {
        var now = Interlocked.Increment(ref _concurrent);
        int seen;
        do {
            seen = _maxConcurrent;
        } while (seen < now && Interlocked.CompareExchange(ref _maxConcurrent, now, seen) != seen);

        Thread.Sleep(150);
        Interlocked.Decrement(ref _concurrent);
        return text;
    }
}
