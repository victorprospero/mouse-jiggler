namespace MouseJiggler.ConsoleApp;

/// <summary>Terminal output port, so presentation logic can be tested without a real console.</summary>
internal interface IConsole
{
    void WriteLine(string message);

    void WriteErrorLine(string message);
}
