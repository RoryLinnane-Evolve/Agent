using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "math_average", Name = "Math:Average", Description = "Computes the average of a comma-separated list of numbers.")]
public static class MathAverageTool
{
    [ToolLogic]
    public static double Average(
        [ToolParam(Description = "Comma-separated list of numbers (e.g., 1,2,3.5)")] string numbers)
    {
        numbers = numbers ?? string.Empty;
        var parts = numbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return 0d;
        double sum = 0d;
        int count = 0;
        foreach (var p in parts)
        {
            if (double.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                sum += v;
                count++;
            }
        }
        return count == 0 ? 0d : sum / count;
    }
}
