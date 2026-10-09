using MouseJiggler.ConsoleApp.CommandLine;

namespace MouseJiggler.ConsoleApp.Tests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Parse_WithNoArguments_SucceedsWithoutOverrides()
    {
        var result = CommandLineParser.Parse([]);

        result.IsSuccess.ShouldBeTrue();
        result.Arguments.ShouldBe(CommandLineArguments.None);
    }

    [Fact]
    public void Parse_WithAllOptions_ReadsEachValue()
    {
        var result = CommandLineParser.Parse(["--interval", "2", "--duration", "7", "--stop-at", "17:30"]);

        result.Arguments.ShouldBe(new CommandLineArguments(
            IntervalSeconds: 2, DurationSeconds: 7, StopAt: new TimeOnly(17, 30), ShowHelp: false));
    }

    [Fact]
    public void Parse_WithSomeOptions_LeavesTheOthersUnset()
    {
        var result = CommandLineParser.Parse(["--duration", "30"]);

        result.Arguments.ShouldBe(CommandLineArguments.None with { DurationSeconds = 30 });
    }

    [Theory]
    [InlineData("18:00", 18, 0)]
    [InlineData("9:05", 9, 5)]
    [InlineData("00:00", 0, 0)]
    public void Parse_WithStopAt_ReadsTheTimeOfDay(string text, int hour, int minute)
    {
        var result = CommandLineParser.Parse(["--stop-at", text]);

        result.Arguments.ShouldBe(CommandLineArguments.None with { StopAt = new TimeOnly(hour, minute) });
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("18")]
    [InlineData("6pm")]
    [InlineData("18:00:00")]
    public void Parse_WithInvalidStopAt_FailsShowingTheExpectedFormat(string text)
    {
        var result = CommandLineParser.Parse(["--stop-at", text]);

        result.Error.ShouldBe("Option '--stop-at' must be a time of day as HH:mm (e.g. 18:00).");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Parse_WithHelpFlag_RequestsHelp(string flag)
    {
        var result = CommandLineParser.Parse([flag]);

        result.Arguments!.ShowHelp.ShouldBeTrue();
    }

    [Theory]
    [InlineData("--speed")]
    [InlineData("--distance")]
    public void Parse_WithUnknownOption_FailsNamingTheOption(string option)
    {
        var result = CommandLineParser.Parse([option, "3"]);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe($"Unknown option '{option}'.");
    }

    [Fact]
    public void Parse_WithMissingValue_Fails()
    {
        var result = CommandLineParser.Parse(["--interval"]);

        result.Error.ShouldBe("Option '--interval' requires a value.");
    }

    [Theory]
    [InlineData("--interval", "abc", "Option '--interval' must be a positive number of seconds.")]
    [InlineData("--interval", "0", "Option '--interval' must be a positive number of seconds.")]
    [InlineData("--interval", "-1", "Option '--interval' must be a positive number of seconds.")]
    [InlineData("--duration", "NaN", "Option '--duration' must be a positive number of seconds.")]
    public void Parse_WithInvalidNumber_FailsExplainingTheExpectedValue(string option, string value, string expectedError)
    {
        var result = CommandLineParser.Parse([option, value]);

        result.Error.ShouldBe(expectedError);
    }

    [Fact]
    public void Parse_UsesTheInvariantCultureForDecimals()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
        try
        {
            CommandLineParser.Parse(["--interval", "0.5"]).Arguments!.IntervalSeconds.ShouldBe(0.5);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
