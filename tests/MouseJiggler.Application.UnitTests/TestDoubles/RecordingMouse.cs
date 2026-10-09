using MouseJiggler.Application.Ports;
using MouseJiggler.Domain;

namespace MouseJiggler.Application.UnitTests.TestDoubles;

/// <summary>An in-memory pointer that records every move it is asked to make.</summary>
internal sealed class RecordingMouse(ScreenPoint startPosition) : IMouseController
{
    private readonly List<ScreenPoint> _moves = [];

    public ScreenPoint Position { get; private set; } = startPosition;

    public IReadOnlyList<ScreenPoint> Moves => _moves;

    public ScreenPoint GetPosition() => Position;

    public void MoveTo(ScreenPoint point)
    {
        _moves.Add(point);
        Position = point;
    }
}
