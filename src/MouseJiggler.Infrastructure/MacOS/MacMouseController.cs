using System.Runtime.Versioning;
using MouseJiggler.Application.Ports;
using MouseJiggler.Domain;
using static MouseJiggler.Infrastructure.MacOS.NativeMethods;

namespace MouseJiggler.Infrastructure.MacOS;

/// <summary>
/// Moves the pointer by posting real mouse-moved HID events. CGWarpMouseCursorPosition is not used
/// on purpose: warping repositions the cursor without resetting the idle timer.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacMouseController : IMouseController
{
    public ScreenPoint GetPosition()
    {
        using var snapshot = CGEventCreate(source: 0);
        EnsureCreated(snapshot, nameof(CGEventCreate));

        var location = CGEventGetLocation(snapshot);
        return new ScreenPoint(location.X, location.Y);
    }

    public void MoveTo(ScreenPoint point)
    {
        using var move = CGEventCreateMouseEvent(source: 0, MouseMovedEvent, new CGPoint(point.X, point.Y), LeftMouseButton);
        EnsureCreated(move, nameof(CGEventCreateMouseEvent));

        CGEventPost(HidEventTap, move);
    }

    private static void EnsureCreated(CFTypeHandle cgEvent, string function)
    {
        if (cgEvent.IsInvalid)
        {
            throw new InvalidOperationException($"CoreGraphics {function} returned NULL.");
        }
    }
}
