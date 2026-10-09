namespace MouseJiggler.ConsoleApp.CommandLine;

/// <summary>Values given on the command line; null means "use the configured value".</summary>
internal sealed record CommandLineArguments(
    double? IntervalSeconds,
    double? DurationSeconds,
    TimeOnly? StopAt,
    bool ShowHelp)
{
    public static CommandLineArguments None { get; } = new(null, null, null, ShowHelp: false);
}
