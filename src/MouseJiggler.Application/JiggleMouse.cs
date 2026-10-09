using MouseJiggler.Application.Ports;
using MouseJiggler.Domain;

namespace MouseJiggler.Application;

/// <summary>
/// Use case: post a mouse-moved event at the pointer's current position. The real input event
/// resets the system idle timer (screen lock, Teams presence) without visibly moving the cursor.
/// </summary>
public sealed class JiggleMouse(IMouseController mouse)
{
    /// <summary>Returns the position the event was posted at.</summary>
    public ScreenPoint Execute()
    {
        var position = mouse.GetPosition();
        mouse.MoveTo(position);
        return position;
    }
}
