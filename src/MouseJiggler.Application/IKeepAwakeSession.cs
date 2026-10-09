using MouseJiggler.Application.Ports;

namespace MouseJiggler.Application;

/// <summary>Input port of the keep-awake use case, used by presentation layers.</summary>
public interface IKeepAwakeSession
{
    /// <summary>Lets callers report a missing permission before announcing that the session started.</summary>
    PointerReadiness CheckReadiness();

    /// <summary>Runs until cancelled or the schedule's time limit (duration and/or stop time) is reached; re-checks readiness first.</summary>
    Task<KeepAwakeOutcome> RunAsync(JiggleSchedule schedule, CancellationToken cancellationToken);
}
