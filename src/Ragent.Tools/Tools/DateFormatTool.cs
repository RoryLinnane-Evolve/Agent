using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "date_format", Name = "DateTime:Format", Description = "Formats an ISO 8601 date/time string using a .NET format string (UTC).")]
public static class DateFormatTool
{
    [ToolLogic]
    public static string Format(
        [ToolParam(Description = "The date/time in ISO 8601 format")] string isoDate,
        [ToolParam(Description = ".NET date/time format string, e.g., yyyy-MM-dd HH:mm:ss")] string format)
    {
        if (!System.DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return "Invalid date format";
        format ??= "O";
        try
        {
            return dt.ToUniversalTime().ToString(format);
        }
        catch
        {
            return "Invalid format string";
        }
    }
}
