namespace MouseJiggler.ConsoleApp;

internal static class ExitCodes
{
    public const int Success = 0;

    /// <summary>Unsupported OS or missing macOS Accessibility permission.</summary>
    public const int PointerUnavailable = 1;

    public const int InvalidArguments = 2;
}
