namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dugouts: sunken, set back from the dirt, stairs down.
/// HarborKit, StarMeter, and the still-gate all read these.
/// Side bleachers sit at |X|≈102. The old pavilion was at 42 / 21.4 on the path.
/// </summary>
public static class HarborDugout
{
    /// <summary>Center X. 1B is +X, 3B is −X.</summary>
    public const float X = 70f;

    /// <summary>Center Z, down the line. Aligned with the side bleacher bank.</summary>
    public const float Z = 40f;

    public const float HalfAlong = 8f;
    public const float HalfDeep = 5.4f;

    /// <summary>Floor below field grade. Cartoon-readable pit, not a shed on the grass.</summary>
    public const float PitDepth = 2.6f;

    /// <summary>Gold fascia above field, not above the pit floor. StarMeter sits here.</summary>
    public const float FasciaY = 4.18f;

    public const float StarSpacing = 2.55f;

    public const int StairCount = 5;
    public const float StairDepth = 0.82f;

    public static float StarZ0 => Z - HalfAlong + 1.7f;

    public static float FieldX(float x) => x > 0f ? x - HalfDeep : x + HalfDeep;

    public static float PitFloorY => -PitDepth;

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
