using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MouseJiggler.Application;
using MouseJiggler.Application.Ports;
using MouseJiggler.ConsoleApp.Configuration;
using MouseJiggler.ConsoleApp.Tests.TestDoubles;
using MouseJiggler.Domain;

namespace MouseJiggler.ConsoleApp.Tests;

public sealed class JigglerAppTests
{
    private readonly RecordingConsole _console = new();
    private readonly IKeepAwakeSession _session = Substitute.For<IKeepAwakeSession>();
    private static readonly StopTime SixPm = new(new TimeOnly(18, 0));

    private readonly JigglerOptions _options = new();

    // Local time zone is UTC: 09:00, well before the default 18:00 stop time.
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero));

    public JigglerAppTests()
    {
        _session.CheckReadiness().Returns(PointerReadiness.Ready);
        _session.RunAsync(Arg.Any<JiggleSchedule>(), Arg.Any<CancellationToken>())
            .Returns(KeepAwakeOutcome.Completed(3));
    }

    private JigglerApp CreateApp() => new(_console, Options.Create(_options), _session, _time);

    [Fact]
    public async Task RunAsync_WithoutArguments_RunsTheSessionWithTheConfiguredSchedule()
    {
        var exitCode = await CreateApp().RunAsync([], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.Success);
        await _session.Received(1).RunAsync(
            new JiggleSchedule(TimeSpan.FromSeconds(10), stopAt: SixPm), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithCommandLineOverrides_RunsTheSessionWithThem()
    {
        await CreateApp().RunAsync(["--interval", "2", "--duration", "7"], CancellationToken.None);

        await _session.Received(1).RunAsync(
            new JiggleSchedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(7), SixPm),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PassesTheHostCancellationTokenToTheSession()
    {
        using var ctrlC = new CancellationTokenSource();

        await CreateApp().RunAsync([], ctrlC.Token);

        await _session.Received(1).RunAsync(Arg.Any<JiggleSchedule>(), ctrlC.Token);
    }

    [Fact]
    public async Task RunAsync_WhenStarting_TellsTheUserWhatHappensAndHowToStop()
    {
        await CreateApp().RunAsync([], CancellationToken.None);

        _console.Output[0].ShouldBe("Keeping the system awake: posting an in-place mouse event every 10 s until 18:00. Press Ctrl+C to stop.");
    }

    [Fact]
    public async Task RunAsync_WithADuration_MentionsWhenItWillStop()
    {
        await CreateApp().RunAsync(["--interval", "2", "--duration", "7"], CancellationToken.None);

        _console.Output[0].ShouldBe("Keeping the system awake: posting an in-place mouse event every 2 s for 7 s or until 18:00, whichever comes first. Press Ctrl+C to stop.");
    }

    [Fact]
    public async Task RunAsync_WithoutAStopTime_SaysItRunsUntilStopped()
    {
        _options.StopAt = "";

        await CreateApp().RunAsync([], CancellationToken.None);

        _console.Output[0].ShouldBe("Keeping the system awake: posting an in-place mouse event every 10 s. Press Ctrl+C to stop.");
    }

    [Fact]
    public async Task RunAsync_WithStopAtOnTheCommandLine_UsesItInsteadOfTheConfiguredOne()
    {
        await CreateApp().RunAsync(["--stop-at", "17:30"], CancellationToken.None);

        await _session.Received(1).RunAsync(
            new JiggleSchedule(TimeSpan.FromSeconds(10), stopAt: new StopTime(new TimeOnly(17, 30))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenTheStopTimeHasAlreadyPassed_SaysSoAndExitsWithoutRunning()
    {
        _time.SetUtcNow(new DateTimeOffset(2026, 10, 6, 18, 30, 0, TimeSpan.Zero));

        var exitCode = await CreateApp().RunAsync([], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.Success);
        _console.Output.ShouldBe(["It is already past 18:00, so there is nothing to keep awake. Use --stop-at HH:mm to choose a later time."]);
        await _session.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_WhenTheSessionEnds_ReportsHowManyJigglesWereMade()
    {
        await CreateApp().RunAsync([], CancellationToken.None);

        _console.Output[^1].ShouldBe("Stopped after 3 jiggle(s).");
    }

    [Fact]
    public async Task RunAsync_WhenThePointerIsNotReady_PrintsOnlyTheProblemAndFailsWithoutRunning()
    {
        _session.CheckReadiness().Returns(PointerReadiness.NotReady("Grant Accessibility permission."));

        var exitCode = await CreateApp().RunAsync([], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.PointerUnavailable);
        _console.Errors.ShouldBe(["Grant Accessibility permission."]);
        _console.Output.ShouldBeEmpty();
        await _session.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_WhenTheSessionStillCannotStart_PrintsTheProblemAndFails()
    {
        _session.RunAsync(Arg.Any<JiggleSchedule>(), Arg.Any<CancellationToken>())
            .Returns(KeepAwakeOutcome.NotReady("Permission was revoked."));

        var exitCode = await CreateApp().RunAsync([], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.PointerUnavailable);
        _console.Errors.ShouldBe(["Permission was revoked."]);
    }

    [Fact]
    public async Task RunAsync_WithInvalidArguments_PrintsTheErrorAndUsageWithoutRunning()
    {
        var exitCode = await CreateApp().RunAsync(["--interval", "fast"], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        _console.Errors[0].ShouldBe("Option '--interval' must be a positive number of seconds.");
        _console.Errors.ShouldContain(Usage.Text);
        await _session.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_WithHelp_PrintsUsageWithoutRunning()
    {
        var exitCode = await CreateApp().RunAsync(["--help"], CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.Success);
        _console.Output.ShouldBe([Usage.Text]);
        await _session.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Fact]
    public void Usage_DocumentsEveryOptionAndItsDefault()
    {
        Usage.Text.ShouldContain("--interval <seconds>");
        Usage.Text.ShouldContain("--duration <seconds>");
        Usage.Text.ShouldContain("--stop-at <HH:mm>");
        Usage.Text.ShouldContain("default: 18:00");
        Usage.Text.ShouldContain("--help");
        Usage.Text.ShouldContain("default: 10");
        Usage.Text.ShouldNotContain("--distance");
    }
}
