namespace MouseJiggler.ConsoleApp.CommandLine;

internal sealed record CommandLineParseResult(CommandLineArguments? Arguments, string? Error)
{
    public bool IsSuccess => Error is null;

    public static CommandLineParseResult Success(CommandLineArguments arguments) => new(arguments, null);

    public static CommandLineParseResult Failure(string error) => new(null, error);
}
