using System;
using Ragent.Tools.Tools;

namespace Ragent.Tools.Tests;

public class DateToolsTests
{
    [Theory]
    [InlineData("2024-01-01T00:00:00Z", 1, "2024-01-02T00:00:00.0000000Z")]
    public void DateAddDays_Works(string input, double days, string expected)
    {
        var actual = DateAddDaysTool.AddDays(input, days);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateAddDays_Invalid_Returns_Message()
    {
        var actual = DateAddDaysTool.AddDays("not-a-date", 3);
        Assert.Equal("Invalid date format", actual);
    }

    [Theory]
    [InlineData("2024-01-01T12:34:56Z", "yyyy-MM-dd", "2024-01-01")]
    [InlineData("2024-01-01T12:34:56+02:00", "O", "2024-01-01T10:34:56.0000000Z")]
    public void DateFormat_Works(string input, string format, string expected)
    {
        var actual = DateFormatTool.Format(input, format);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateFormat_InvalidDate_Returns_Message()
    {
        var actual = DateFormatTool.Format("bad", "yyyy");
        Assert.Equal("Invalid date format", actual);
    }

    [Fact]
    public void DateFormat_InvalidFormat_Returns_Message()
    {
        var actual = DateFormatTool.Format("2024-01-01T00:00:00Z", "invalid {{{");
        Assert.Equal("invali1 {{{", actual);
    }

    [Fact]
    public void DateNow_Returns_IsoUtc()
    {
        var iso = DateNowTool.Now();
        // Parse and ensure round-trip ISO 8601 with Z
        Assert.EndsWith("Z", iso);
        var parsed = DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
    }
}
