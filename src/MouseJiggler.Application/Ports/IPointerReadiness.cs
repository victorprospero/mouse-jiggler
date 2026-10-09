namespace MouseJiggler.Application.Ports;

/// <summary>Tells whether this process may drive the pointer on the current platform
/// (e.g. macOS Accessibility permission, unsupported operating system).</summary>
public interface IPointerReadiness
{
    PointerReadiness Check();
}

public sealed record PointerReadiness(bool IsReady, string? Problem)
{
    public static PointerReadiness Ready { get; } = new(true, null);

    public static PointerReadiness NotReady(string problem) => new(false, problem);
}
