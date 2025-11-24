using Ragent.Reflection;

namespace SampleApp.Tools;

[Tool(Description = "This tool returns the square root of a number.", Id = "square_root", Name = "Square Root")]
public class SquareRoot {
    [ToolLogic]
    public static double Logic([ToolParam(Description = "The number you want the square root of.")]double number) {
        return Math.Sqrt(number);
    }
}