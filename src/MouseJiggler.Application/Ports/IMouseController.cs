using MouseJiggler.Domain;

namespace MouseJiggler.Application.Ports;

/// <summary>Reads and moves the system pointer. Implementations must generate real input events
/// (not just reposition the cursor) so the OS idle timer is reset. Adapters for platforms that
/// cannot drive the pointer throw <see cref="PlatformNotSupportedException"/>; callers check
/// <see cref="IPointerReadiness"/> first.</summary>
public interface IMouseController
{
    ScreenPoint GetPosition();

    void MoveTo(ScreenPoint point);
}
