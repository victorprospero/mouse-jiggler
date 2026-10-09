namespace MouseJiggler.Domain.UnitTests;

public sealed class StopTimeTests
{
    private static readonly TimeZoneInfo BrasiliaLike =
        TimeZoneInfo.CreateCustomTimeZone("UTC-3", TimeSpan.FromHours(-3), "UTC-3", "UTC-3");

    private static readonly StopTime SixPm = new(new TimeOnly(18, 0));

    [Fact]
    public void RemainingFrom_BeforeTheStopTime_IsTheTimeLeftToday()
    {
        var now = new DateTimeOffset(2026, 10, 6, 9, 30, 0, TimeSpan.FromHours(-3));

        SixPm.RemainingFrom(now, BrasiliaLike).ShouldBe(new TimeSpan(8, 30, 0));
    }

    [Fact]
    public void RemainingFrom_ANowInAnotherOffset_UsesTheLocalClock()
    {
        // 12:00 UTC is 09:00 in UTC-3.
        var now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

        SixPm.RemainingFrom(now, BrasiliaLike).ShouldBe(TimeSpan.FromHours(9));
    }

    [Theory]
    [InlineData(18, 0)]
    [InlineData(18, 1)]
    [InlineData(23, 59)]
    public void RemainingFrom_AtOrAfterTheStopTime_IsNotPositive(int hour, int minute)
    {
        var now = new DateTimeOffset(2026, 10, 6, hour, minute, 0, TimeSpan.FromHours(-3));

        SixPm.RemainingFrom(now, BrasiliaLike).ShouldBeLessThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void RemainingFrom_AcrossADaylightSavingChange_CountsRealElapsedTime()
    {
        var newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        // 2026-03-08: clocks jump from 02:00 EST to 03:00 EDT, so 01:00 -> 18:00 is 16 real hours.
        var now = new DateTimeOffset(2026, 3, 8, 1, 0, 0, TimeSpan.FromHours(-5));

        SixPm.RemainingFrom(now, newYork).ShouldBe(TimeSpan.FromHours(16));
    }

    [Fact]
    public void ToString_IsTwentyFourHourClock()
    {
        new StopTime(new TimeOnly(9, 5)).ToString().ShouldBe("09:05");
        SixPm.ToString().ShouldBe("18:00");
    }
}
