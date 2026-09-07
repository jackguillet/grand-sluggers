namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dirt language: paths and pads, not a lake. Bags as bags.
/// Lawn holes stay on <see cref="HarborDugout"/>. HarborKit reads these.
/// </summary>
public static class HarborInfield
{
    public const float PathWidth = ParkDiamond.PathWidth;
    public const float BagDirtR = ParkDiamond.BagDirtR;
    public const float BagSize = ParkDiamond.BagSize;
    public const float BagY = ParkDiamond.BagY;
    public const float HomePackedR = ParkDiamond.HomePackedR;

    public static bool PathIsNotALake() => ParkDiamond.PathIsNotALake();

    public static bool BagIsABag() => ParkDiamond.BagIsABag();

    public static bool HomePackedIsAPad() => ParkDiamond.HomePackedIsAPad();

    /// <summary>Infield lawn must not recap a dugout well.</summary>
    public static bool LawnRespectsPits() =>
        !HarborDugout.InPitHole(0, 63.64) && HarborDugout.LawnCovers(0, 63.64);
}
