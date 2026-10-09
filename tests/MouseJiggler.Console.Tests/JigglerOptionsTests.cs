using MouseJiggler.ConsoleApp.CommandLine;
using MouseJiggler.ConsoleApp.Configuration;
using MouseJiggler.Domain;

namespace MouseJiggler.ConsoleApp.Tests;

public sealed class JigglerOptionsTests
{
    [Fact]
    public void Defaults_AreTenSecondsStoppingAtSixPm()
    {
        var options = new JigglerOptions();

        options.IntervalSeconds.ShouldBe(10);
        options.DurationSeconds.ShouldBeNull();
        options.StopAt.ShouldBe("18:00");
    }

    [Fact]
    public void ToSchedule_WithoutCommandLineOverrides_UsesTheConfiguredValues()
    {
        var options = new JigglerOptions { IntervalSeconds = 30, DurationSeconds = 60, StopAt = "17:45" };

        var schedule = options.ToSchedule(CommandLineArguments.None);

        schedule.Interval.ShouldBe(TimeSpan.FromSeconds(30));
        schedule.Duration.ShouldBe(TimeSpan.FromSeconds(60));
        schedule.StopAt.ShouldBe(new StopTime(new TimeOnly(17, 45)));
    }

    [Fact]
    public void ToSchedule_WithCommandLineOverrides_PrefersTheCommandLine()
    {
        var options = new JigglerOptions { IntervalSeconds = 30 };
        var overrides = new CommandLineArguments(IntervalSeconds: 2, DurationSeconds: 7, StopAt: new TimeOnly(19, 15), ShowHelp: false);

        var schedule = options.ToSchedule(overrides);

        schedule.Interval.ShouldBe(TimeSpan.FromSeconds(2));
        schedule.Duration.ShouldBe(TimeSpan.FromSeconds(7));
        schedule.StopAt.ShouldBe(new StopTime(new TimeOnly(19, 15)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ToSchedule_WithAnEmptyStopAt_HasNoStopTime(string? stopAt)
    {
        var schedule = new JigglerOptions { StopAt = stopAt }.ToSchedule(CommandLineArguments.None);

        schedule.StopAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("7:30")]
    public void Validate_WithAnEmptyOrValidStopAt_Succeeds(string? stopAt)
    {
        new JigglerOptionsValidator().Validate(null, new JigglerOptions { StopAt = stopAt }).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithDefaults_Succeeds()
    {
        var result = new JigglerOptionsValidator().Validate(null, new JigglerOptions());

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNonPositiveValues_ReportsEachInvalidSetting()
    {
        var options = new JigglerOptions { IntervalSeconds = 0, DurationSeconds = 0, StopAt = "6 pm" };

        var result = new JigglerOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldBe(
        [
            "Jiggler:IntervalSeconds must be a positive number.",
            "Jiggler:DurationSeconds must be a positive number when set.",
            "Jiggler:StopAt must be a time of day as HH:mm (e.g. 18:00), or empty for no stop time.",
        ]);
    }
}
