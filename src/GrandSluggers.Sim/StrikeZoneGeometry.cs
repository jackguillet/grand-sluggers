namespace GrandSluggers.Sim;

/// <summary>World-space bounds of the displayed arcade zone at the plate crossing.</summary>
public static class StrikeZoneGeometry
{
    public const double HalfWidth = 0.92;
    public const double Bottom = 1.45;
    public const double Top = 3.65;
    public const double PlateZ = 0;
    public const double CenterY = (Bottom + Top) / 2;
    public const double Height = Top - Bottom;

    public static bool Contains(double x, double y) =>
        double.IsFinite(x) && double.IsFinite(y)
        && Math.Abs(x) <= HalfWidth && y >= Bottom && y <= Top;

    public static bool Contains(PitchCommand pitch, string? starPitchId = null)
    {
        var p = PitchFlight.Point(pitch, 1, starPitchId);
        return Contains(p.X, p.Y);
    }
}
