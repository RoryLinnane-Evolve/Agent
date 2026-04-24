using Ragent.Reflection;

namespace SampleApp.Tools;

[ToolCollection]
public class SquareRoot {
    [Tool(Id = "square_root", Name = "Square Root", Description = "This tool returns the square root of a number.")]
    public static double Logic([ToolParam(Description = "The number you want the square root of.")] double number) {
        return Math.Sqrt(number);
    }
}
