using System.Runtime.Versioning;
using MouseJiggler.Domain;
using MouseJiggler.Infrastructure.MacOS;

namespace MouseJiggler.Infrastructure.IntegrationTests;

[SupportedOSPlatform("macos")]
public sealed class MacMouseControllerTests
{
    [MacOSFact]
    public void GetPosition_ReturnsFiniteCoordinates()
    {
        var position = new MacMouseController().GetPosition();

        double.IsFinite(position.X).ShouldBeTrue();
        double.IsFinite(position.Y).ShouldBeTrue();
    }

    [AccessibilityFact]
    public void MoveTo_WithAccessibilityPermission_MovesTheRealPointer()
    {
        var mouse = new MacMouseController();
        var origin = mouse.GetPosition();
        var target = origin with { Y = origin.Y >= 50 ? origin.Y - 20 : origin.Y + 20 };

        try
        {
            mouse.MoveTo(target);

            WaitForPointerAt(mouse, target).Y.ShouldBe(target.Y, tolerance: 1);
        }
        finally
        {
            mouse.MoveTo(origin);
        }
    }

    [AccessibilityFact]
    public void MoveTo_TheCurrentPosition_ResetsTheSystemIdleTimerWithoutMovingThePointer()
    {
        var mouse = new MacMouseController();
        var position = mouse.GetPosition();
        Thread.Sleep(TimeSpan.FromSeconds(1.5));
        SystemIdleTime.Seconds.ShouldBeGreaterThan(1, "the test needs ~1.5 s without real input; don't touch the mouse/keyboard");

        mouse.MoveTo(position);

        WaitForIdleBelow(TimeSpan.FromSeconds(0.5)).ShouldBeLessThan(0.5);
        mouse.GetPosition().ShouldBe(position);
    }

    /// <summary>CGEventPost only enqueues the event; the window server applies it a few milliseconds later.</summary>
    private static ScreenPoint WaitForPointerAt(MacMouseController mouse, ScreenPoint target)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var position = mouse.GetPosition();
        while (Math.Abs(position.Y - target.Y) > 1 && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
            position = mouse.GetPosition();
        }

        return position;
    }

    private static double WaitForIdleBelow(TimeSpan threshold)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var idle = SystemIdleTime.Seconds;
        while (idle >= threshold.TotalSeconds && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
            idle = SystemIdleTime.Seconds;
        }

        return idle;
    }
}
