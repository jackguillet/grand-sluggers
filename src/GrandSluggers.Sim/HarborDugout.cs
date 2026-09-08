namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dugouts: the hip wall <b>is</b> the rail. Pit behind it toward the
/// stands, roof over the bench, stairs at the home end. HarborKit dresses these.
/// </summary>
public static class HarborDugout
{
    const float Inv = 0.70710678f;

    /// <summary>Midpoint along the 90-ft baseline (home → bag).</summary>
    public const float Along0 = 52f;

    /// <summary>Center X. 1B is +X, 3B is −X. HalfDeep behind the hip wall.</summary>
    public const float X = 64.7f;

    /// <summary>Center Z. Spans just after home to just before the bag.</summary>
    public const float Z = 8.84f;

    /// <summary>Two-thirds of the first home-to-bag span.</summary>
    public const float HalfAlong = 21.3f;
    public const float HalfDeep = 3.5f;

    /// <summary>Floor below field grade. Half-underground like a big-league pit.</summary>
    public const float PitDepth = 3.2f;

    /// <summary>Gold fascia = hip wall height so the rail continues the short wall.</summary>
    public const float FasciaY = 4.2f;

    public const float StarSpacing = 2.55f;

    public const int StairCount = 4;
    public const float StairDepth = 0.7f;

    /// <summary>Steps stay in the home-end opening, not a runway onto the grass.</summary>
    public const float FieldStairRun = 1.2f;

    /// <summary>Rail (local −X of the kit) faces the diamond. 180° from the stands-facing drop.</summary>
    public static float YawDeg(int sign) => sign > 0 ? -135f : -45f;

    public static float StarZ0 => Z - HalfAlong + 1.7f;

    /// <summary>World X of the field-side rail (the hip wall).</summary>
    public static float FieldX(float x) => x > 0f ? RailX(1) : RailX(-1);

    public static float RailX(int sign) => sign * Inv * (Along0 + HarborWall.FoulOffset);

    public static float RailZ() => Inv * (Along0 - HarborWall.FoulOffset);

    /// <summary>Hip-wall point at this distance along the baseline.</summary>
    public static (float X, float Z) RailAt(int sign, float along)
    {
        var off = HarborWall.FoulOffset;
        return (sign * (Inv * along + Inv * off), Inv * along - Inv * off);
    }

    public static float PitFloorY => -PitDepth;

    /// <summary>Lawn must not cover this box (pit + field stairs). Pad so the lip reads.</summary>
    public const float HolePad = 1.2f;

    public static float HoleMinX => FieldX(X) - FieldStairRun - HolePad;
    public static float HoleMaxX => X + HalfDeep + HolePad;
    public static float HoleMinZ => Z - HalfAlong - HolePad;
    public static float HoleMaxZ => Z + HalfAlong + HolePad;

    public static bool InPitHole(double x, double z)
    {
        var ax = Math.Abs(x);
        var along = (ax + z) * Inv;
        var into = (ax - z) * Inv;
        return Math.Abs(along - Along0) <= HalfAlong + HolePad
            && Math.Abs(into - (HarborWall.FoulOffset + HalfDeep)) <= HalfDeep + FieldStairRun + HolePad;
    }

    /// <summary>Z span of the pit at this X, for punching a lawn hole on a 45° dugout.</summary>
    public static bool TryHoleZ(double x, out float z0, out float z1)
    {
        z0 = z1 = 0;
        const double s2 = 1.41421356237;
        var ax = Math.Abs(x);
        var ha = (HalfAlong + HolePad) * s2;
        var hd = (HalfDeep + FieldStairRun + HolePad) * s2;
        var a0 = X + Z - ax - ha;
        var a1 = X + Z - ax + ha;
        var b0 = ax - X + Z - hd;
        var b1 = ax - X + Z + hd;
        z0 = (float)Math.Max(a0, b0);
        z1 = (float)Math.Min(a1, b1);
        return z1 > z0 + 1f;
    }

    public static bool LawnCovers(double x, double z) => !InPitHole(x, z);

    /// <summary>
    /// The short wall opens here: DressWall skips the hip boxes so the
    /// padded rail is the wall along home-to-bag.
    /// </summary>
    public static bool WallOpensHere(double x, double z)
    {
        var ax = Math.Abs(x);
        var along = (ax + z) * Inv;
        var into = (ax - z) * Inv;
        return along > Along0 - HalfAlong + 1f
            && along < Along0 + HalfAlong - 1f
            && Math.Abs(into - HarborWall.FoulOffset) < 5f
            && z < 95;
    }

    /// <summary>Front rail sits on the hip wall, pit behind it into foul.</summary>
    public static bool RailIsTheHipWall()
    {
        var into = Math.Abs(X - Z) / 1.41421356f;
        return Math.Abs(into - (HarborWall.FoulOffset + HalfDeep)) < 0.8f
            && Math.Abs(FasciaY - HarborWall.HipHeight) < 0.15f;
    }

    /// <summary>Field-side lip is the hip wall, past the 11-ft dirt path.</summary>
    public static bool IsSetBackFromTheDirt() =>
        HarborWall.FoulOffset > 12f;

    /// <summary>Just past the plate dirt, not on the chalk.</summary>
    public static bool StartsAfterHome() =>
        Along0 - HalfAlong > 12f;

    /// <summary>Stops short of the 90-ft bag.</summary>
    public static bool EndsBeforeTheBag() =>
        Along0 + HalfAlong < Diamond.Baseline - 4;

    public static bool IsSunken() => PitDepth >= 2f;

    public static bool HasStairs() => StairCount >= 4 && StairDepth > 0.4f;

    /// <summary>MLB pit: padded rail and mesh front, not a wooden shed.</summary>
    public static bool HasMeshFront() => FasciaY > 3.2f && FasciaY < 5.0f && PitDepth >= 2f;

    /// <summary>
    /// Scoop / plate cameras must not sit inside either dugout box (roof included).
    /// </summary>
    public static bool CameraClears(double camX, double camZ)
    {
        return !InPitHole(camX, camZ);
    }
}
