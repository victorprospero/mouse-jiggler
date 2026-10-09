using MouseJiggler.Domain;

namespace MouseJiggler.Application;

/// <summary>
/// How often to jiggle, and optionally when to stop: after a duration, at a local time of day,
/// or whichever comes first (neither = until stopped).
/// </summary>
public sealed record JiggleSchedule
{
    public JiggleSchedule(TimeSpan interval, TimeSpan? duration = null, StopTime? stopAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        if (duration is { } bounded)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bounded, TimeSpan.Zero, nameof(duration));
        }

        Interval = interval;
        Duration = duration;
        StopAt = stopAt;
    }

    public TimeSpan Interval { get; }

    public TimeSpan? Duration { get; }

    public StopTime? StopAt { get; }

    /// <summary>How long a session starting now may run; null = no limit, zero or negative = the stop time has passed.</summary>
    public TimeSpan? TimeLimit(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var untilStop = StopAt?.RemainingFrom(clock.GetUtcNow(), clock.LocalTimeZone);

        return (Duration, untilStop) switch
        {
            ({ } duration, { } remaining) => duration < remaining ? duration : remaining,
            (var duration, var remaining) => duration ?? remaining,
        };
    }
}
