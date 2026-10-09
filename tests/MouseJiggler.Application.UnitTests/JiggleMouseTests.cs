using MouseJiggler.Application.UnitTests.TestDoubles;
using MouseJiggler.Domain;

namespace MouseJiggler.Application.UnitTests;

public sealed class JiggleMouseTests
{
    [Theory]
    [InlineData(300, 500)]
    [InlineData(0, 0)]
    [InlineData(-1084, 1296)]
    public void Execute_PostsASingleMoveAtTheCurrentPosition_SoThePointerDoesNotVisiblyMove(double x, double y)
    {
        var position = new ScreenPoint(x, y);
        var mouse = new RecordingMouse(position);

        new JiggleMouse(mouse).Execute();

        mouse.Moves.ShouldBe([position]);
    }

    [Fact]
    public void Execute_ReturnsThePositionTheEventWasPostedAt()
    {
        var jiggleMouse = new JiggleMouse(new RecordingMouse(new ScreenPoint(300, 500)));

        jiggleMouse.Execute().ShouldBe(new ScreenPoint(300, 500));
    }
}
