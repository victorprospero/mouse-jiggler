using System.Runtime.Versioning;
using MouseJiggler.Infrastructure.MacOS;

namespace MouseJiggler.Infrastructure.IntegrationTests;

public sealed class PointerReadinessTests
{
    [Fact]
    public void UnsupportedPlatform_Check_IsNotReadyAndNamesTheSupportedPlatform()
    {
        var readiness = new UnsupportedPlatformPointer().Check();

        readiness.IsReady.ShouldBeFalse();
        readiness.Problem.ShouldNotBeNull();
        readiness.Problem.ShouldContain("Unsupported platform");
        readiness.Problem.ShouldContain("macOS");
    }

    [Fact]
    public void UnsupportedPlatform_MoveTo_ThrowsPlatformNotSupported()
    {
        Action move = () => new UnsupportedPlatformPointer().MoveTo(new Domain.ScreenPoint(0, 0));

        move.ShouldThrow<PlatformNotSupportedException>();
    }

    [MacOSFact]
    [SupportedOSPlatform("macos")]
    public void MacOS_Check_WhenNotTrusted_ExplainsHowToGrantAccessibility()
    {
        var readiness = new MacPointerReadiness().Check();

        if (readiness.IsReady)
        {
            readiness.Problem.ShouldBeNull();
            return;
        }

        readiness.Problem.ShouldNotBeNull();
        readiness.Problem.ShouldContain("System Settings > Privacy & Security > Accessibility");
    }
}
