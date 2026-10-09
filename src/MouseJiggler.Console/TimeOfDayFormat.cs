using System.Globalization;

namespace MouseJiggler.ConsoleApp;

/// <summary>The 24-hour <c>HH:mm</c> format accepted by <c>--stop-at</c> and <c>Jiggler:StopAt</c>.</summary>
internal static class TimeOfDayFormat
{
    public const string Description = "a time of day as HH:mm (e.g. 18:00)";

    private const string Format = "H:mm";

    public static bool TryParse(string? text, out TimeOnly time) =>
        TimeOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    /// <summary>For values already checked by <see cref="TryParse"/> (e.g. validated options); throws otherwise.</summary>
    public static TimeOnly Parse(string text) => TimeOnly.ParseExact(text, Format, CultureInfo.InvariantCulture);
}
