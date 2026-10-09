using System.Runtime.Versioning;
using MouseJiggler.Application.Ports;
using static MouseJiggler.Infrastructure.MacOS.NativeMethods;

namespace MouseJiggler.Infrastructure.MacOS;

/// <summary>macOS silently drops synthetic input events unless the host app has Accessibility permission.</summary>
[SupportedOSPlatform("macos")]
public sealed class MacPointerReadiness : IPointerReadiness
{
    internal const string AccessibilityGuidance =
        "macOS has not granted Accessibility permission to this app, so it cannot move the mouse." + "\n" +
        "Open System Settings > Privacy & Security > Accessibility, enable the app you run this from " +
        "(e.g. Terminal, iTerm2 or Visual Studio Code), then quit and reopen that app and try again.";

    public PointerReadiness Check() =>
        AXIsProcessTrusted() ? PointerReadiness.Ready : PointerReadiness.NotReady(AccessibilityGuidance);
}
