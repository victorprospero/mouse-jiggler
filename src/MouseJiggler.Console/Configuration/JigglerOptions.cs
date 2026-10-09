using MouseJiggler.Application;
using MouseJiggler.ConsoleApp.CommandLine;
using MouseJiggler.Domain;

namespace MouseJiggler.ConsoleApp.Configuration;

/// <summary>The "Jiggler" section of appsettings.json.</summary>
internal sealed class JigglerOptions
{
    public const string SectionName = "Jiggler";
    public const double DefaultIntervalSeconds = 10;
    public const string DefaultStopAt = "18:00";

    public double IntervalSeconds { get; set; } = DefaultIntervalSeconds;

    /// <summary>Stop automatically after this many seconds; null runs until Ctrl+C.</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>Local time of day (HH:mm) to stop at; null or empty means no stop time.</summary>
    public string? StopAt { get; set; } = DefaultStopAt;

    /// <summary>Builds the use-case input, letting command-line values override configuration.</summary>
    public JiggleSchedule ToSchedule(CommandLineArguments overrides)
    {
        var duration = overrides.DurationSeconds ?? DurationSeconds;
        var stopAt = overrides.StopAt ?? ConfiguredStopAt();

        return new JiggleSchedule(
            TimeSpan.FromSeconds(overrides.IntervalSeconds ?? IntervalSeconds),
            duration is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
            stopAt is { } time ? new StopTime(time) : null);
    }

    private TimeOnly? ConfiguredStopAt() =>
        string.IsNullOrWhiteSpace(StopAt) ? null : TimeOfDayFormat.Parse(StopAt);
}
