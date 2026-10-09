using System.Globalization;

namespace MouseJiggler.Domain;

/// <summary>A local time of day at which keeping the system awake ends (e.g. 18:00, the end of the work day).</summary>
public sealed record StopTime(TimeOnly TimeOfDay)
{
    /// <summary>
    /// Real time left until today's stop time on the local clock of <paramref name="zone"/>;
    /// zero or negative once it has passed (it never rolls over to tomorrow).
    /// </summary>
    public TimeSpan RemainingFrom(DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var stopLocal = DateOnly.FromDateTime(localNow.DateTime).ToDateTime(TimeOfDay);
        var stopInstant = new DateTimeOffset(stopLocal, zone.GetUtcOffset(stopLocal));

        return stopInstant - now;
    }

    public override string ToString() => TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture);
}
