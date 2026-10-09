using Microsoft.Extensions.Time.Testing;
using MouseJiggler.Domain;

namespace MouseJiggler.Application.UnitTests;

public sealed class JiggleScheduleTests
{
    [Fact]
    public void Constructor_WithPositiveIntervalAndNoDuration_RunsUntilStopped()
    {
        var schedule = new JiggleSchedule(TimeSpan.FromSeconds(10));

        schedule.Interval.ShouldBe(TimeSpan.FromSeconds(10));
        schedule.Duration.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveInterval_Throws(double seconds)
    {
        Action create = () => _ = new JiggleSchedule(TimeSpan.FromSeconds(seconds));

        create.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithNonPositiveDuration_Throws(double seconds)
    {
        Action create = () => _ = new JiggleSchedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(seconds));

        create.ShouldThrow<ArgumentOutOfRangeException>();
    }

    private static readonly StopTime SixPm = new(new TimeOnly(18, 0));

    // FakeTimeProvider's local time zone is UTC.
    private static FakeTimeProvider At(int hour, int minute) => new(new DateTimeOffset(2026, 10, 6, hour, minute, 0, TimeSpan.Zero));

    [Fact]
    public void TimeLimit_WithoutDurationOrStopTime_IsNone()
    {
        new JiggleSchedule(TimeSpan.FromSeconds(10)).TimeLimit(At(9, 0)).ShouldBeNull();
    }

    [Fact]
    public void TimeLimit_WithOnlyADuration_IsTheDuration()
    {
        var schedule = new JiggleSchedule(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));

        schedule.TimeLimit(At(9, 0)).ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void TimeLimit_WithOnlyAStopTime_IsTheTimeLeftUntilIt()
    {
        var schedule = new JiggleSchedule(TimeSpan.FromSeconds(10), stopAt: SixPm);

        schedule.TimeLimit(At(17, 15)).ShouldBe(TimeSpan.FromMinutes(45));
    }

    [Theory]
    [InlineData(17, 0, 30)]   // the 30 min duration ends first
    [InlineData(17, 50, 10)]  // 18:00 comes first
    public void TimeLimit_WithBoth_IsWhicheverComesFirst(int hour, int minute, int expectedMinutes)
    {
        var schedule = new JiggleSchedule(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(30), SixPm);

        schedule.TimeLimit(At(hour, minute)).ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Fact]
    public void TimeLimit_AfterTheStopTime_IsNotPositive()
    {
        var schedule = new JiggleSchedule(TimeSpan.FromSeconds(10), stopAt: SixPm);

        schedule.TimeLimit(At(18, 30))!.Value.ShouldBeLessThanOrEqualTo(TimeSpan.Zero);
    }
}
