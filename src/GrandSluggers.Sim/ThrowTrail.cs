namespace GrandSluggers.Sim;

/// <summary>
/// Spec §15: an ordinary throw is the ball. Chemistry tints the sphere trail
/// (<see cref="BallRgb"/>), never a predicted line through the dirt.
/// Catalog slots of kind <see cref="Kind"/> stay; they do not draw a destination laser.
/// </summary>
public static class ThrowTrail
{
    public const string Kind = "throw";

    /// <summary>
    /// Throw-kind VFX never draw a release-to-bag line. The next catalog trail inherits this.
    /// </summary>
    public const bool DestinationLine = false;

    public static bool IsThrow(NamedSlot vfx) =>
        string.Equals(vfx.Kind, Kind, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<NamedSlot> Slots(ArtCatalog? art)
    {
        if (art == null) return [];
        var list = new List<NamedSlot>();
        foreach (var vfx in art.Vfx)
        {
            if (IsThrow(vfx)) list.Add(vfx);
        }
        return list;
    }

    public static bool DrawsDestinationLine(NamedSlot vfx) =>
        IsThrow(vfx) && DestinationLine;

    public static int DestinationPositions(NamedSlot vfx) =>
        DrawsDestinationLine(vfx) ? 12 : 0;

    public static (double R, double G, double B) BallRgb(Chemistry rel) =>
        CartoonJuice.ThrowRgb(rel);
}
