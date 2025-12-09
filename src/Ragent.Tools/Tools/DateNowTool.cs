using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "date_now", Name = "DateTime:Now", Description = "Returns the current UTC date and time in ISO 8601 format.")]
public static class DateNowTool
{
    [ToolLogic]
    public static string Now()
        => System.DateTime.UtcNow.ToString("O");
}
