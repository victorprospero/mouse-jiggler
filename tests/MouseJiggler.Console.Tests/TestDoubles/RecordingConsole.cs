namespace MouseJiggler.ConsoleApp.Tests.TestDoubles;

internal sealed class RecordingConsole : IConsole
{
    public List<string> Output { get; } = [];

    public List<string> Errors { get; } = [];

    public void WriteLine(string message) => Output.Add(message);

    public void WriteErrorLine(string message) => Errors.Add(message);
}
