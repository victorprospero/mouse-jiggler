using Microsoft.Extensions.Logging.Testing;
using MouseJiggler.Application.Ports;
using MouseJiggler.Application.UnitTests.TestDoubles;
using MouseJiggler.Domain;

namespace MouseJiggler.Application.UnitTests;

public sealed class KeepAwakeSessionTests
{
    private static readonly ScreenPoint Start = new(300, 500);
    private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10);

    private readonly RecordingMouse _mouse = new(Start);
    private readonly IPointerReadiness _readiness = Substitute.For<IPointerReadiness>();
    private readonly ObservableTimeProvider _time = new();
    private readonly FakeLogger<KeepAwakeSession> _logger = new();

    public KeepAwakeSessionTests() => _readiness.Check().Returns(PointerReadiness.Ready);

    private KeepAwakeSession CreateSession() =>
        new(new JiggleMouse(_mouse), _readiness, _time, _logger);

    private static JiggleSchedule EveryTenSeconds(TimeSpan? duration = null) =>
        new(TenSeconds, duration);

    [Fact]
    public async Task RunAsync_WhenPointerIsNotReady_ReturnsTheProblemWithoutMovingTheMouse()
    {
        _readiness.Check().Returns(PointerReadiness.NotReady("Grant Accessibility permission."));

        var outcome = await CreateSession().RunAsync(EveryTenSeconds(), CancellationToken.None);

        outcome.ShouldBe(KeepAwakeOutcome.NotReady("Grant Accessibility permission."));
        _mouse.Moves.ShouldBeEmpty();
    }

    [Fact]
    public void CheckReadiness_ReportsWhatThePlatformSays()
    {
        _readiness.Check().Returns(PointerReadiness.NotReady("No permission."));

        CreateSession().CheckReadiness().ShouldBe(PointerReadiness.NotReady("No permission."));
    }

    [Fact]
    public async Task RunAsync_WhenStarted_JigglesImmediately()
    {
        using var stop = new CancellationTokenSource();

        var run = CreateSession().RunAsync(EveryTenSeconds(), stop.Token);

        _mouse.Moves.ShouldBe([Start]);
        await stop.CancelAsync();
        await run;
    }

    [Fact]
    public async Task RunAsync_EachTimeTheIntervalElapses_JigglesAgainWithoutMovingThePointer()
    {
        using var stop = new CancellationTokenSource();
        var run = CreateSession().RunAsync(EveryTenSeconds(), stop.Token);
        await _time.WaitForTimersCreatedAsync(1);

        _time.Advance(TimeSpan.FromSeconds(9));
        JiggleCount().ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(1));
        await _time.WaitForTimersCreatedAsync(2);
        JiggleCount().ShouldBe(2);

        _time.Advance(TenSeconds);
        await _time.WaitForTimersCreatedAsync(3);
        JiggleCount().ShouldBe(3);
        _mouse.Moves.ShouldAllBe(move => move == Start);

        await stop.CancelAsync();
        await run;
    }

    [Fact]
    public async Task RunAsync_WithADuration_StopsWhenItElapsesAndReportsTheJiggleCount()
    {
        // Timers: #1 is the duration limit, then one per interval delay.
        var run = CreateSession().RunAsync(EveryTenSeconds(duration: TimeSpan.FromSeconds(25)), CancellationToken.None);
        await _time.WaitForTimersCreatedAsync(2);
        _time.Advance(TenSeconds);
        await _time.WaitForTimersCreatedAsync(3);
        _time.Advance(TenSeconds);
        await _time.WaitForTimersCreatedAsync(4);

        _time.Advance(TimeSpan.FromSeconds(5));
        var outcome = await run;

        outcome.ShouldBe(KeepAwakeOutcome.Completed(3));
        _time.Advance(TenSeconds);
        JiggleCount().ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_StopsGracefullyAndReportsTheJiggleCount()
    {
        using var stop = new CancellationTokenSource();
        var run = CreateSession().RunAsync(EveryTenSeconds(), stop.Token);
        await _time.WaitForTimersCreatedAsync(1);
        _time.Advance(TenSeconds);
        await _time.WaitForTimersCreatedAsync(2);

        await stop.CancelAsync();
        var outcome = await run;

        outcome.ShouldBe(KeepAwakeOutcome.Completed(2));
    }

    [Fact]
    public async Task RunAsync_WhenAlreadyCancelled_DoesNotJiggle()
    {
        var outcome = await CreateSession().RunAsync(EveryTenSeconds(), new CancellationToken(canceled: true));

        outcome.ShouldBe(KeepAwakeOutcome.Completed(0));
        _mouse.Moves.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ForEachJiggle_LogsOneInformationLine()
    {
        var run = CreateSession().RunAsync(EveryTenSeconds(duration: TimeSpan.FromSeconds(15)), CancellationToken.None);
        await _time.WaitForTimersCreatedAsync(2);
        _time.Advance(TenSeconds);
        await _time.WaitForTimersCreatedAsync(3);
        _time.Advance(TimeSpan.FromSeconds(5));
        await run;

        var jiggleLogs = _logger.Collector.GetSnapshot();
        jiggleLogs.Count.ShouldBe(2);
        jiggleLogs.ShouldAllBe(log => log.Level == Microsoft.Extensions.Logging.LogLevel.Information);
        jiggleLogs[0].Message.ShouldBe("Jiggle #1: input event posted in place at (300, 500)");
        jiggleLogs[1].Message.ShouldStartWith("Jiggle #2:");
    }

    [Fact]
    public async Task RunAsync_WithAStopTime_StopsWhenTheClockReachesIt()
    {
        var time = new ObservableTimeProvider(new DateTimeOffset(2026, 10, 6, 17, 59, 35, TimeSpan.Zero));
        var session = new KeepAwakeSession(new JiggleMouse(_mouse), _readiness, time, _logger);
        var schedule = new JiggleSchedule(TenSeconds, stopAt: new StopTime(new TimeOnly(18, 0)));

        // Timers: #1 is the 25 s left until 18:00, then one per interval delay.
        var run = session.RunAsync(schedule, CancellationToken.None);
        await time.WaitForTimersCreatedAsync(2);
        time.Advance(TenSeconds);
        await time.WaitForTimersCreatedAsync(3);
        time.Advance(TenSeconds);
        await time.WaitForTimersCreatedAsync(4);
        time.Advance(TimeSpan.FromSeconds(5));

        (await run).ShouldBe(KeepAwakeOutcome.Completed(3));
    }

    [Fact]
    public async Task RunAsync_WhenTheStopTimeHasAlreadyPassed_DoesNotJiggle()
    {
        var time = new ObservableTimeProvider(new DateTimeOffset(2026, 10, 6, 18, 30, 0, TimeSpan.Zero));
        var session = new KeepAwakeSession(new JiggleMouse(_mouse), _readiness, time, _logger);

        var outcome = await session.RunAsync(
            new JiggleSchedule(TenSeconds, stopAt: new StopTime(new TimeOnly(18, 0))), CancellationToken.None);

        outcome.ShouldBe(KeepAwakeOutcome.Completed(0));
        _mouse.Moves.ShouldBeEmpty();
    }

    private int JiggleCount() => _mouse.Moves.Count;
}
