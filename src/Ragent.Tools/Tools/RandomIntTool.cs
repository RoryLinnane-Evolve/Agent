using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "rand_int", Name = "Random:Int", Description = "Generates a random integer between min (inclusive) and max (exclusive). Optional seed for determinism.")]
public static class RandomIntTool
{
    [ToolLogic]
    public static int Next(
        [ToolParam(Description = "Minimum value (inclusive)")] int min,
        [ToolParam(Description = "Maximum value (exclusive)")] int max,
        [ToolParam(Description = "Optional seed for deterministic output (0 = no seed)")] int seed)
    {
        if (max <= min) return min;
        var rng = seed == 0 ? new Random() : new Random(seed);
        return rng.Next(min, max);
    }
}
