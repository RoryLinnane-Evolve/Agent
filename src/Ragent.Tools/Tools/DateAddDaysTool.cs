using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "date_add_days", Name = "DateTime:AddDays", Description = "Adds a number of days to a given ISO date/time string and returns ISO 8601 UTC.")]
public static class DateAddDaysTool
{
    [ToolLogic]
    public static string AddDays(
        [ToolParam(Description = "The base date/time in ISO 8601 (e.g., 2024-01-01T00:00:00Z)")] string isoDate,
        [ToolParam(Description = "Number of days to add (can be negative)")] double days)
    {
        if (!System.DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return "Invalid date format";
        var res = dt.ToUniversalTime().AddDays(days);
        return res.ToString("O");
    }
}
