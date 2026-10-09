using System.Globalization;

namespace MouseJiggler.Domain.UnitTests;

public sealed class ScreenPointTests
{
    [Theory]
    [InlineData(300, 500, "(300, 500)")]
    [InlineData(-1084.6, 1296.4, "(-1085, 1296)")]
    public void ToString_RoundsToWholePoints(double x, double y, string expected)
    {
        new ScreenPoint(x, y).ToString().ShouldBe(expected);
    }

    [Fact]
    public void ToString_IgnoresTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            new ScreenPoint(1234, 5678).ToString().ShouldBe("(1234, 5678)");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
