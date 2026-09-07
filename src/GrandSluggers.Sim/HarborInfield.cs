namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dirt language: paths and pads, not a lake. Bags as bags.
/// Lawn holes stay on <see cref="HarborDugout"/>. HarborKit reads these.
/// </summary>
public static class HarborInfield
{
    /// <summary>Dirt path width (ft). 11 ate the grass Y.</summary>
    public const float PathWidth = 8f;

    /// <summary>Packed dirt circle around each bag. Not an 11-ft cylinder.</summary>
    public const float BagDirtR = 5.2f;

    /// <summary>White bag edge. Square, diamond-aligned.</summary>
    public const float BagSize = 1.85f;
    public const float BagY = 0.22f;

    /// <summary>Packed dirt around the plate. Not a 34-ft oval.</summary>
    public const float HomePackedR = 16f;

    public static bool PathIsNotALake() => PathWidth < 11f;

    public static bool BagIsABag() => BagSize < 2.2f && BagDirtR < 7f && BagDirtR > BagSize;

    public static bool HomePackedIsAPad() => HomePackedR < 34f;

    /// <summary>Infield lawn must not recap a dugout well.</summary>
    public static bool LawnRespectsPits() =>
        !HarborDugout.InPitHole(0, 63.64) && HarborDugout.LawnCovers(0, 63.64);
}
