using CargoHub.Api.Services;
using Xunit;

namespace CargoHub.Tests.Services;

public class DailyDigestTimeHelperTests
{
    [Fact]
    public void GetUtcRangeForLocalDate_Utc_YesterdaySpans24Hours()
    {
        var (from, to) = DailyDigestTimeHelper.GetUtcRangeForLocalDate(new DateOnly(2025, 3, 10), "UTC");
        Assert.Equal(new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2025, 3, 11, 0, 0, 0, DateTimeKind.Utc), to);
    }

    [Fact]
    public void TryParseRunAtLocalTime_Valid_ReturnsTrue()
    {
        Assert.True(DailyDigestTimeHelper.TryParseRunAtLocalTime("00:05", out var t));
        Assert.Equal(new TimeSpan(0, 5, 0), t);
    }

    [Fact]
    public void TryParseRunAtLocalTime_Invalid_ReturnsFalse()
    {
        Assert.False(DailyDigestTimeHelper.TryParseRunAtLocalTime("25:00", out _));
        Assert.False(DailyDigestTimeHelper.TryParseRunAtLocalTime("bad", out _));
    }
}
