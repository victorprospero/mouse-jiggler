using System.Globalization;
using Microsoft.Extensions.Options;
using MouseJiggler.Application;
using MouseJiggler.ConsoleApp.CommandLine;
using MouseJiggler.ConsoleApp.Configuration;

namespace MouseJiggler.ConsoleApp;

/// <summary>Presentation: turns command-line input into a keep-awake session and reports the result.</summary>
internal sealed class JigglerApp(
    IConsole console, IOptions<JigglerOptions> options, IKeepAwakeSession session, TimeProvider timeProvider)
{
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var parsed = CommandLineParser.Parse(args);
        if (!parsed.IsSuccess)
        {
            console.WriteErrorLine(parsed.Error!);
            console.WriteErrorLine(Usage.Text);
            return ExitCodes.InvalidArguments;
        }

        var arguments = parsed.Arguments!;
        if (arguments.ShowHelp)
        {
            console.WriteLine(Usage.Text);
            return ExitCodes.Success;
        }

        var readiness = session.CheckReadiness();
        if (!readiness.IsReady)
        {
            return ReportPointerUnavailable(readiness.Problem!);
        }

        var schedule = options.Value.ToSchedule(arguments);
        if (schedule.TimeLimit(timeProvider) <= TimeSpan.Zero)
        {
            console.WriteLine($"It is already past {schedule.StopAt}, so there is nothing to keep awake. Use --stop-at HH:mm to choose a later time.");
            return ExitCodes.Success;
        }

        console.WriteLine(DescribeStart(schedule));

        var outcome = await session.RunAsync(schedule, cancellationToken).ConfigureAwait(false);
        if (!outcome.HasRun)
        {
            return ReportPointerUnavailable(outcome.Problem!);
        }

        console.WriteLine($"Stopped after {outcome.JiggleCount} jiggle(s).");
        return ExitCodes.Success;
    }

    private int ReportPointerUnavailable(string problem)
    {
        console.WriteErrorLine(problem);
        return ExitCodes.PointerUnavailable;
    }

    private static string DescribeStart(JiggleSchedule schedule)
    {
        var limit = (schedule.Duration, schedule.StopAt) switch
        {
            ({ } duration, { } stopAt) => $" for {Seconds(duration)} or until {stopAt}, whichever comes first",
            ({ } duration, null) => $" for {Seconds(duration)}",
            (null, { } stopAt) => $" until {stopAt}",
            _ => string.Empty,
        };
        return $"Keeping the system awake: posting an in-place mouse event every {Seconds(schedule.Interval)}{limit}. Press Ctrl+C to stop.";
    }

    private static string Seconds(TimeSpan span) =>
        span.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture) + " s";
}
