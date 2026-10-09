using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MouseJiggler.Application;
using MouseJiggler.Application.Ports;
using MouseJiggler.ConsoleApp.Configuration;
using MouseJiggler.ConsoleApp.Tests.TestDoubles;

namespace MouseJiggler.ConsoleApp.Tests;

public sealed class CompositionRootTests
{
    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?>? settings = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.AddMouseJiggler();
        if (settings is not null)
        {
            builder.Configuration.AddInMemoryCollection(settings);
        }

        return builder;
    }

    [Fact]
    public void AddMouseJiggler_WiresEveryDependencyOfTheApp()
    {
        using var host = CreateBuilder().Build();

        host.Services.GetRequiredService<JigglerApp>().ShouldNotBeNull();
        host.Services.GetRequiredService<IKeepAwakeSession>().ShouldBeOfType<KeepAwakeSession>();
        host.Services.GetRequiredService<IConsole>().ShouldBeOfType<SystemConsole>();
    }

    [Fact]
    public void AddMouseJiggler_ReadsTheShippedAppSettings()
    {
        using var host = CreateBuilder().Build();

        var options = host.Services.GetRequiredService<IOptions<JigglerOptions>>().Value;

        options.IntervalSeconds.ShouldBe(JigglerOptions.DefaultIntervalSeconds);
        options.DurationSeconds.ShouldBeNull();
        options.StopAt.ShouldBe(JigglerOptions.DefaultStopAt);
    }

    [Fact]
    public void AddMouseJiggler_BindsTheJigglerSection()
    {
        using var host = CreateBuilder(new() { ["Jiggler:IntervalSeconds"] = "5", ["Jiggler:DurationSeconds"] = "60" }).Build();

        var options = host.Services.GetRequiredService<IOptions<JigglerOptions>>().Value;

        options.IntervalSeconds.ShouldBe(5);
        options.DurationSeconds.ShouldBe(60);
    }

    [Fact]
    public void AddMouseJiggler_WithInvalidConfiguration_FailsValidation()
    {
        using var host = CreateBuilder(new() { ["Jiggler:IntervalSeconds"] = "0" }).Build();

        var read = () => host.Services.GetRequiredService<IOptions<JigglerOptions>>().Value;

        read.ShouldThrow<OptionsValidationException>().Message.ShouldContain("Jiggler:IntervalSeconds");
    }

    [Fact]
    public async Task RunMouseJigglerAsync_WhenTheHostIsAskedToStop_EndsTheSessionGracefully()
    {
        // No stop time, so the test does not depend on the time of day it runs at.
        var builder = CreateBuilder(new() { ["Jiggler:StopAt"] = "" });
        var console = new RecordingConsole();
        builder.Services.AddSingleton<IConsole>(console);
        var session = new RunsUntilCancelledSession();
        builder.Services.AddSingleton<IKeepAwakeSession>(session);
        using var host = builder.Build();

        var run = host.RunMouseJigglerAsync([]);
        await session.Started.WaitAsync(TimeSpan.FromSeconds(5));
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        var exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));

        exitCode.ShouldBe(ExitCodes.Success);
        console.Output[^1].ShouldBe("Stopped after 0 jiggle(s).");
    }

    private sealed class RunsUntilCancelledSession : IKeepAwakeSession
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public PointerReadiness CheckReadiness() => PointerReadiness.Ready;

        public async Task<KeepAwakeOutcome> RunAsync(JiggleSchedule schedule, CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => { }, TaskScheduler.Default);
            return KeepAwakeOutcome.Completed(0);
        }
    }
}
