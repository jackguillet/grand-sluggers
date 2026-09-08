namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dugouts: sunken, set back from the dirt, stairs down.
/// HarborKit, StarMeter, and the still-gate all read these.
/// Side bleachers sit at |X|≈102. The old pavilion was at 42 / 21.4 on the path.
/// </summary>
public static class HarborDugout
{
    /// <summary>Center X. 1B is +X, 3B is −X. Foul, against the hip wall.</summary>
    public const float X = 52f;

    /// <summary>Center Z. Spans just after home to just before the bag.</summary>
    public const float Z = 21f;

    public const float HalfAlong = 32f;
    public const float HalfDeep = 5.2f;

    /// <summary>Floor below field grade. Cartoon-readable pit, not a shed on the grass.</summary>
    public const float PitDepth = 2.4f;

    /// <summary>Gold fascia above field, not above the pit floor. StarMeter sits here.</summary>
    public const float FasciaY = 3.15f;

    public const float StarSpacing = 2.55f;

    public const int StairCount = 4;
    public const float StairDepth = 0.7f;

    /// <summary>Steps are in the pit lip, not a runway onto the grass.</summary>
    public const float FieldStairRun = 1.2f;

    /// <summary>1B open toward the diamond along the foul line. 3B mirrored.</summary>
    public static float YawDeg(int sign) => sign > 0 ? -45f : -135f;

    public static float StarZ0 => Z - HalfAlong + 1.7f;

    public static float FieldX(float x) => x > 0f ? x - HalfDeep : x + HalfDeep;

    public static float PitFloorY => -PitDepth;

    /// <summary>Lawn must not cover this box (pit + field stairs). Pad so the lip reads.</summary>
    public const float HolePad = 1.2f;

    public static float HoleMinX => FieldX(X) - FieldStairRun - HolePad;
    public static float HoleMaxX => X + HalfDeep + HolePad;
    public static float HoleMinZ => Z - HalfAlong - HolePad;
    public static float HoleMaxZ => Z + HalfAlong + HolePad;

    public static bool InPitHole(double x, double z)
    {
        const double inv = 0.7071067811865476;
        var ax = Math.Abs(x);
        var along = (ax + z) * inv;
        var into = (ax - z) * inv;
        var along0 = (X + Z) * inv;
        var into0 = (X - Z) * inv;
        return Math.Abs(along - along0) <= HalfAlong + HolePad
            && Math.Abs(into - into0) <= HalfDeep + FieldStairRun + HolePad;
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

    /// <summary>Field-side lip is past the 11-ft dirt path on the 45° line.</summary>
    public static bool IsSetBackFromTheDirt() =>
        Math.Abs(X - Z) / 1.41421356f > 12f;

    /// <summary>Just past the plate dirt, not on the chalk.</summary>
    public static bool StartsAfterHome() =>
        (X + Z) * 0.70710678f - HalfAlong > 12f;

    /// <summary>Stops short of the 90-ft bag.</summary>
    public static bool EndsBeforeTheBag() =>
        (X + Z) * 0.70710678f + HalfAlong < Diamond.Baseline - 4;

    public static bool IsSunken() => PitDepth >= 2f;

    public static bool HasStairs() => StairCount >= 4 && StairDepth > 0.4f;

    /// <summary>MLB pit: padded rail and mesh front, not a wooden shed.</summary>
    public static bool HasMeshFront() => FasciaY > 2.6f && FasciaY < 4.0f && PitDepth >= 2f;

    /// <summary>
    /// Scoop / plate cameras must not sit inside either dugout box (roof included).
    /// </summary>
    public static bool CameraClears(double camX, double camZ)
    {
        var pad = 3.0;
        var minZ = Z - HalfAlong - pad;
        var maxZ = Z + HalfAlong + pad;
        var minX = X - HalfDeep - pad;
        var maxX = X + HalfDeep + pad;
        var in1B = camX > minX && camX < maxX && camZ > minZ && camZ < maxZ;
        var in3B = camX < -minX && camX > -maxX && camZ > minZ && camZ < maxZ;
        return !in1B && !in3B;
    }
}
