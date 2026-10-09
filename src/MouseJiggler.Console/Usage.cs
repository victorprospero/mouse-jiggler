using MouseJiggler.ConsoleApp.Configuration;

namespace MouseJiggler.ConsoleApp;

internal static class Usage
{
    public static string Text { get; } = $"""
        Usage: MouseJiggler [options]

        Posts a mouse event at the pointer's current position on a fixed interval, so the system never
        goes idle (screen stays unlocked, Teams stays Available). The cursor does not visibly move.

        Options:
          --interval <seconds>  Time between jiggles (default: {JigglerOptions.DefaultIntervalSeconds}, or {JigglerOptions.SectionName}:IntervalSeconds in appsettings.json)
          --duration <seconds>  Stop automatically after this long (default: none)
          --stop-at <HH:mm>     Stop automatically at this local time (default: {JigglerOptions.DefaultStopAt}, or {JigglerOptions.SectionName}:StopAt; empty = none)
          -h, --help            Show this help
        """;
}
