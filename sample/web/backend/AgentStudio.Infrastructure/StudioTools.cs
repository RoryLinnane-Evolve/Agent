using System.Globalization;
using Ragent.Reflection;

namespace AgentStudio.Infrastructure;

[ToolCollection]
public static class StudioTools
{
    [Tool(Id = "calculate", Name = "Calculate", Description = "Performs one basic arithmetic operation on two numbers.")]
    public static string Calculate(
        [ToolParam(Description = "The first number")] double left,
        [ToolParam(Description = "One of: add, subtract, multiply, divide")] string operation,
        [ToolParam(Description = "The second number")] double right)
    {
        var result = operation.ToLowerInvariant() switch
        {
            "add" => left + right,
            "subtract" => left - right,
            "multiply" => left * right,
            "divide" when right != 0 => left / right,
            "divide" => throw new DivideByZeroException("Cannot divide by zero."),
            _ => throw new ArgumentException("Operation must be add, subtract, multiply, or divide.", nameof(operation))
        };

        return result.ToString(CultureInfo.InvariantCulture);
    }

    [Tool(Id = "current_utc_time", Name = "Current UTC time", Description = "Returns the current UTC date and time in ISO 8601 format.")]
    public static string GetCurrentUtcTime() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    [Tool(Id = "word_count", Name = "Word count", Description = "Counts the words in a supplied piece of text.")]
    public static string CountWords([ToolParam(Description = "The text to count")] string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length.ToString(CultureInfo.InvariantCulture);
}
