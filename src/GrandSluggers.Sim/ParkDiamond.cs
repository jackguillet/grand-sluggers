namespace GrandSluggers.Sim;

/// <summary>
/// Shared diamond ground: dirt paths, mound, striped grass, foul poles, warning track.
/// Harbor fills these now. Other parks reuse the same outlines when they get a kit —
/// fence distances come from <see cref="Park"/>, bags and the rubber from <see cref="Diamond"/>.
/// </summary>
public static class ParkDiamond
{
    /// <summary>Dirt path width (ft). 11 ate the grass Y.</summary>
    public const float PathWidth = 8f;
    /// <summary>Center of the dirt slab. Must sit above <see cref="GrassTop"/> so the ring reads at couch.</summary>
    public const float PathY = 0.26f;
    public const float PathThick = 0.24f;
    /// <summary>Dirt top minus grass top. A 0.05-ft lip z-fights and vanishes under the lawn.</summary>
    public const float PathLip = 0.18f;
    /// <summary>Rounded diamond corners at the bags. Bigger than half the path so the ring reads round, not a cross.</summary>
    public const float PathCornerR = 14f;

    /// <summary>Packed dirt circle around each bag. Not an 11-ft cylinder.</summary>
    public const float BagDirtR = 5.2f;

    /// <summary>White bag edge. Square, diamond-aligned.</summary>
    public const float BagSize = 1.85f;
    public const float BagY = 0.22f;

    /// <summary>Packed dirt around the plate. Not a 34-ft oval.</summary>
    public const float HomePackedR = 16f;

    /// <summary>One smooth dirt hill. Not stacked cylinders.</summary>
    public const float MoundR = 9.2f;
    public const float MoundH = 0.98f;
    /// <summary>Flat crown the rubber sits on. Wider than the rubber, smaller than the hill.</summary>
    public const float MoundTableR = 2.2f;
    public const float RubberY = 1.02f;
    public const float RubberW = 1.7f;
    public const float RubberH = 0.07f;
    public const float RubberD = 0.42f;

    /// <summary>Dirt band inside the wall. Must stay past the infield lip.</summary>
    public const float TrackWidth = 15f;
    public const int TrackSegs = 36;
    public const float TrackY = 0.14f;
    public const float TrackThick = 0.22f;

    public const float PoleHeight = 52f;
    public const float PoleRadius = 0.85f;
    public const float PoleScreenH = 16f;
    public const float PoleScreenW = 7f;
    public const float PoleScreenY = 38f;

    /// <summary>Mow stripe width. Bands of constant X — home → CF, vertical in the overhead.</summary>
    public const float StripeWidth = 18f;
    public const float GrassY = 0.08f;
    public const float GrassThick = 0.12f;
    public const float GrassZ0 = -28f;
    /// <summary>Foul-territory grass past the 45° line.</summary>
    public const float FoulGrassFt = 36f;

    public static bool PathIsNotALake() => PathWidth < 11f;

    public static bool BagIsABag() => BagSize < 2.2f && BagDirtR < 7f && BagDirtR > BagSize;

    public static bool HomePackedIsAPad() => HomePackedR < 34f;

    public static bool MoundIsAHill() =>
        MoundR > 7f && MoundR < 14f
        && MoundTableR > RubberW * 0.45f && MoundTableR < MoundR * 0.45f
        && MoundH > GrassTop + 0.5f
        && MoundH <= RubberY
        && RubberY - MoundH < 0.08f
        && RubberY > 0.8f && RubberY < 1.4f;

    public static bool StripeReadsAtCouch() => StripeWidth >= 12f && StripeWidth <= 28f;

    public static bool PathCornersAreRound() =>
        PathCornerR >= PathWidth && PathCornerR < Diamond.Baseline * 0.4f;

    public static float GrassTop => GrassY + GrassThick * 0.5f;
    public static float PathTop => PathY + PathThick * 0.5f;
    public static float PathBottom => PathY - PathThick * 0.5f;

    /// <summary>Dirt ring sits on the lawn, not in it. Grass is drawn first; dirt after.</summary>
    public static bool DirtClearsTheLawn() =>
        PathTop >= GrassTop + PathLip && PathBottom >= GrassTop - 0.02f && PathY > GrassY;

    /// <summary>Stripes are columns along CF (X bands), not rows along 1B–3B.</summary>
    public static bool StripesRunHomeToCf() => true;

    public static (double X, double Z) FoulPole(Park park, int sign)
    {
        var spray = Math.Sign(sign) * AtBatResolver.FoulLineDeg;
        var fence = AtBatResolver.FenceAt(park, spray);
        var rad = spray * Math.PI / 180.0;
        return (Math.Sin(rad) * fence, Math.Cos(rad) * fence);
    }

    public static double TrackMid(Park park, double sprayDeg) =>
        AtBatResolver.FenceAt(park, sprayDeg) - TrackWidth * 0.5;

    public static float GrassHalfWidth(float z) =>
        Math.Max(40f, Math.Abs(z) + FoulGrassFt);

    public static float GrassZ1(Park park) =>
        (float)park.CenterFenceFt - TrackWidth;

    public static bool TrackIsInsideTheWall(Park park) =>
        TrackWidth > 8f && TrackWidth < 24f
        && TrackMid(park, 0) > FieldingResolver.InfieldLipFt
        && TrackMid(park, 0) < park.CenterFenceFt;

    public static bool PoleIsOnTheFoulLine(Park park)
    {
        var (x, z) = FoulPole(park, 1);
        var spray = Math.Atan2(x, z) * (180.0 / Math.PI);
        return Math.Abs(Math.Abs(spray) - AtBatResolver.FoulLineDeg) < 0.6;
    }

    public static bool PoleSitsOnThatParkFence(Park park)
    {
        var (x, z) = FoulPole(park, 1);
        var dist = Diamond.Dist(0, 0, x, z);
        return Math.Abs(dist - park.RightFenceFt) < 1.5;
    }

    public static bool LawnRespectsPits() => HarborInfield.LawnRespectsPits();
}
