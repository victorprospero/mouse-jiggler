using MouseJiggler.Infrastructure.MacOS;

namespace MouseJiggler.Infrastructure.IntegrationTests;

/// <summary>Runs only on macOS.</summary>
public sealed class MacOSFactAttribute : FactAttribute
{
    public MacOSFactAttribute()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Skip = "Requires macOS.";
        }
    }
}

/// <summary>Runs only on macOS when this process may post input events (Accessibility granted),
/// because it really moves the pointer.</summary>
public sealed class AccessibilityFactAttribute : FactAttribute
{
    public AccessibilityFactAttribute()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Skip = "Requires macOS.";
        }
        else if (!new MacPointerReadiness().Check().IsReady)
        {
            Skip = "Requires Accessibility permission for the test runner (System Settings > Privacy & Security > Accessibility).";
        }
    }
}
