using System.Globalization;

namespace MouseJiggler.ConsoleApp.CommandLine;

/// <summary>Parses <c>--interval &lt;s&gt; --duration &lt;s&gt; --stop-at &lt;HH:mm&gt; [--help]</c>.</summary>
internal static class CommandLineParser
{
    private const string PositiveSeconds = "a positive number of seconds";

    private static readonly HashSet<string> HelpFlags = ["--help", "-h"];

    private static readonly Dictionary<string, ValueOption> ValueOptions = new()
    {
        ["--interval"] = new(PositiveSeconds, (arguments, text) =>
            TryParsePositive(text, out var value) ? arguments with { IntervalSeconds = value } : null),
        ["--duration"] = new(PositiveSeconds, (arguments, text) =>
            TryParsePositive(text, out var value) ? arguments with { DurationSeconds = value } : null),
        ["--stop-at"] = new(TimeOfDayFormat.Description, (arguments, text) =>
            TimeOfDayFormat.TryParse(text, out var time) ? arguments with { StopAt = time } : null),
    };

    public static CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        var arguments = CommandLineArguments.None;

        for (var index = 0; index < args.Count; index++)
        {
            var name = args[index];

            if (HelpFlags.Contains(name))
            {
                arguments = arguments with { ShowHelp = true };
                continue;
            }

            if (!ValueOptions.TryGetValue(name, out var option))
            {
                return CommandLineParseResult.Failure($"Unknown option '{name}'.");
            }

            if (++index >= args.Count)
            {
                return CommandLineParseResult.Failure($"Option '{name}' requires a value.");
            }

            if (option.TryApply(arguments, args[index]) is not { } updated)
            {
                return CommandLineParseResult.Failure($"Option '{name}' must be {option.Expected}.");
            }

            arguments = updated;
        }

        return CommandLineParseResult.Success(arguments);
    }

    private static bool TryParsePositive(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value)
        && value > 0;

    /// <summary>An option followed by a value; <see cref="TryApply"/> returns null when the value is invalid.</summary>
    private sealed record ValueOption(string Expected, Func<CommandLineArguments, string, CommandLineArguments?> TryApply);
}
