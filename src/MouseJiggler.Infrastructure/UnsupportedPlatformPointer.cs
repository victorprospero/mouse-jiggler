using System.Runtime.InteropServices;
using MouseJiggler.Application.Ports;
using MouseJiggler.Domain;

namespace MouseJiggler.Infrastructure;

/// <summary>
/// Registered on operating systems without an adapter. Readiness reports the problem so the
/// keep-awake session never starts; the pointer members therefore never run in practice.
/// </summary>
public sealed class UnsupportedPlatformPointer : IPointerReadiness, IMouseController
{
    private static string Problem =>
        $"Unsupported platform: {RuntimeInformation.OSDescription}. MouseJiggler currently supports macOS only.";

    public PointerReadiness Check() => PointerReadiness.NotReady(Problem);

    public ScreenPoint GetPosition() => throw new PlatformNotSupportedException(Problem);

    public void MoveTo(ScreenPoint point) => throw new PlatformNotSupportedException(Problem);
}
