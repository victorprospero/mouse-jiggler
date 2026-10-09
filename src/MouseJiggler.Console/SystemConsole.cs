namespace MouseJiggler.ConsoleApp;

internal sealed class SystemConsole : IConsole
{
    public void WriteLine(string message) => Console.Out.WriteLine(message);

    public void WriteErrorLine(string message) => Console.Error.WriteLine(message);
}
