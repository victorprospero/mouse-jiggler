using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace MouseJiggler.Infrastructure.MacOS;

/// <summary>P/Invoke surface for the macOS frameworks used to read and post pointer events.</summary>
[SupportedOSPlatform("macos")]
internal static partial class NativeMethods
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    /// <summary>kCGEventMouseMoved.</summary>
    public const uint MouseMovedEvent = 5;

    /// <summary>kCGMouseButtonLeft (ignored for move events but required by the API).</summary>
    public const uint LeftMouseButton = 0;

    /// <summary>kCGHIDEventTap: inject where hardware events enter, which resets the system idle timer.</summary>
    public const uint HidEventTap = 0;

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct CGPoint(double X, double Y);

    [LibraryImport(CoreGraphics)]
    public static partial CFTypeHandle CGEventCreate(nint source);

    [LibraryImport(CoreGraphics)]
    public static partial CGPoint CGEventGetLocation(CFTypeHandle cgEvent);

    [LibraryImport(CoreGraphics)]
    public static partial CFTypeHandle CGEventCreateMouseEvent(nint source, uint mouseType, CGPoint position, uint mouseButton);

    [LibraryImport(CoreGraphics)]
    public static partial void CGEventPost(uint tap, CFTypeHandle cgEvent);

    [LibraryImport(CoreFoundation)]
    public static partial void CFRelease(nint cfObject);

    [LibraryImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AXIsProcessTrusted();

    /// <summary>Owns a Core Foundation object (e.g. a CGEventRef) and releases it with CFRelease.</summary>
    public sealed class CFTypeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public CFTypeHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            CFRelease(handle);
            return true;
        }
    }
}
