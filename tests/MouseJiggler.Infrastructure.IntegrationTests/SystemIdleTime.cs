using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MouseJiggler.Infrastructure.IntegrationTests;

/// <summary>Seconds since the last HID input event: the idle time macOS (screen lock) and Teams (presence) look at.</summary>
[SupportedOSPlatform("macos")]
internal static partial class SystemIdleTime
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>kCGEventSourceStateHIDSystemState.</summary>
    private const int HidSystemState = 1;

    /// <summary>kCGAnyInputEventType.</summary>
    private const uint AnyInputEvent = uint.MaxValue;

    public static double Seconds => CGEventSourceSecondsSinceLastEventType(HidSystemState, AnyInputEvent);

    [LibraryImport(CoreGraphics)]
    private static partial double CGEventSourceSecondsSinceLastEventType(int stateId, uint eventType);
}
