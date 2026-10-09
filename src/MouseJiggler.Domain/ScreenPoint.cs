using System.Globalization;

namespace MouseJiggler.Domain;

/// <summary>A position in global screen coordinates (logical points, Y grows downwards).</summary>
public readonly record struct ScreenPoint(double X, double Y)
{
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0}, {Y:0})");
}
