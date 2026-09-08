namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dugouts: sunken, set back from the dirt, stairs down.
/// HarborKit, StarMeter, and the still-gate all read these.
/// Side bleachers sit at |X|≈102. The old pavilion was at 42 / 21.4 on the path.
/// </summary>
public static class HarborDugout
{
    /// <summary>Center X. 1B is +X, 3B is −X. Between home and the bag, in foul.</summary>
    public const float X = 62f;

    /// <summary>Center Z, down the line. Past 45 ft from the plate, short of 1B.</summary>
    public const float Z = 32f;

    public const float HalfAlong = 16f;
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
        if (z < HoleMinZ || z > HoleMaxZ) return false;
        var ax = Math.Abs(x);
        return ax >= HoleMinX && ax <= HoleMaxX;
    }

    public static bool LawnCovers(double x, double z) => !InPitHole(x, z);

    /// <summary>Field-side lip is past the 11-ft dirt path on the 45° line.</summary>
    public static bool IsSetBackFromTheDirt() => FieldX(X) > 52f;

    public static bool IsSunken() => PitDepth >= 2f;

    public static bool HasStairs() => StairCount >= 4 && StairDepth > 0.4f;

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
