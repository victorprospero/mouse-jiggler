using Microsoft.Extensions.Time.Testing;

namespace MouseJiggler.Application.UnitTests.TestDoubles;

/// <summary>
/// Wraps a <see cref="FakeTimeProvider"/> and lets a test wait until the code under test has
/// scheduled a timer. <c>Task.Delay(TimeSpan, TimeProvider, ...)</c> resumes asynchronously, so
/// without this a test could advance the clock before the next delay is even registered.
/// </summary>
internal sealed class ObservableTimeProvider : TimeProvider
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private readonly FakeTimeProvider _clock;
    private readonly Lock _gate = new();
    private readonly List<(int Count, TaskCompletionSource Signal)> _waiters = [];
    private int _timersCreated;

    /// <summary>Starts at <paramref name="start"/> (default: FakeTimeProvider's epoch); the local time zone is UTC.</summary>
    public ObservableTimeProvider(DateTimeOffset? start = null) =>
        _clock = start is { } now ? new FakeTimeProvider(now) : new FakeTimeProvider();

    public override DateTimeOffset GetUtcNow() => _clock.GetUtcNow();

    public override long GetTimestamp() => _clock.GetTimestamp();

    public override long TimestampFrequency => _clock.TimestampFrequency;

    public override TimeZoneInfo LocalTimeZone => _clock.LocalTimeZone;

    public void Advance(TimeSpan delta) => _clock.Advance(delta);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = _clock.CreateTimer(callback, state, dueTime, period);
        lock (_gate)
        {
            _timersCreated++;
            foreach (var waiter in _waiters.Where(w => w.Count <= _timersCreated).ToList())
            {
                waiter.Signal.TrySetResult();
                _waiters.Remove(waiter);
            }
        }

        return timer;
    }

    public Task WaitForTimersCreatedAsync(int count)
    {
        lock (_gate)
        {
            if (_timersCreated >= count)
            {
                return Task.CompletedTask;
            }

            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, signal));
            return signal.Task.WaitAsync(WaitTimeout);
        }
    }
}
