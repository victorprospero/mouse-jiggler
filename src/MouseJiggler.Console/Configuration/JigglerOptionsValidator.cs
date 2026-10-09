using Microsoft.Extensions.Options;

namespace MouseJiggler.ConsoleApp.Configuration;

internal sealed class JigglerOptionsValidator : IValidateOptions<JigglerOptions>
{
    public ValidateOptionsResult Validate(string? name, JigglerOptions options)
    {
        List<string> failures = [];

        if (!IsPositive(options.IntervalSeconds))
        {
            failures.Add($"{JigglerOptions.SectionName}:{nameof(options.IntervalSeconds)} must be a positive number.");
        }

        if (options.DurationSeconds is { } duration && !IsPositive(duration))
        {
            failures.Add($"{JigglerOptions.SectionName}:{nameof(options.DurationSeconds)} must be a positive number when set.");
        }

        if (!string.IsNullOrWhiteSpace(options.StopAt) && !TimeOfDayFormat.TryParse(options.StopAt, out _))
        {
            failures.Add($"{JigglerOptions.SectionName}:{nameof(options.StopAt)} must be {TimeOfDayFormat.Description}, or empty for no stop time.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;
}
