using Microsoft.Extensions.Logging;
using MouseJiggler.Application.Ports;
using MouseJiggler.Domain;

namespace MouseJiggler.Application;

/// <summary>
/// Use case: keep the machine from going idle by posting an in-place pointer event on a schedule until the
/// caller cancels (e.g. Ctrl+C), the optional duration elapses or the optional stop time is reached.
/// </summary>
public sealed partial class KeepAwakeSession(
    JiggleMouse jiggleMouse,
    IPointerReadiness readiness,
    TimeProvider timeProvider,
    ILogger<KeepAwakeSession> logger) : IKeepAwakeSession
{
    private const string UnknownReadinessProblem = "Pointer control is unavailable on this machine.";

    public PointerReadiness CheckReadiness() => readiness.Check();

    public async Task<KeepAwakeOutcome> RunAsync(JiggleSchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var status = CheckReadiness();
        if (!status.IsReady)
        {
            return KeepAwakeOutcome.NotReady(status.Problem ?? UnknownReadinessProblem);
        }

        var timeLimit = schedule.TimeLimit(timeProvider);
        if (timeLimit <= TimeSpan.Zero)
        {
            return KeepAwakeOutcome.Completed(0);
        }

        using var limit = CreateTimeLimit(timeLimit);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, limit.Token);

        var jiggleCount = await JiggleUntilStoppedAsync(schedule, stop.Token).ConfigureAwait(false);

        return KeepAwakeOutcome.Completed(jiggleCount);
    }

    private async Task<int> JiggleUntilStoppedAsync(JiggleSchedule schedule, CancellationToken stopToken)
    {
        var jiggleCount = 0;
        try
        {
            while (!stopToken.IsCancellationRequested)
            {
                var position = jiggleMouse.Execute();
                jiggleCount++;
                LogJiggled(logger, jiggleCount, position);

                await Task.Delay(schedule.Interval, timeProvider, stopToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
        {
            // Stopping is the expected way for a session to end (Ctrl+C or duration elapsed).
        }

        return jiggleCount;
    }

    private CancellationTokenSource CreateTimeLimit(TimeSpan? timeLimit) =>
        timeLimit is { } limit ? new CancellationTokenSource(limit, timeProvider) : new CancellationTokenSource();

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Jiggle #{Number}: input event posted in place at {Position}")]
    private static partial void LogJiggled(ILogger logger, int number, ScreenPoint position);
}
