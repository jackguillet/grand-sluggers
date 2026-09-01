namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dress that reads from the field postcard and from live SET.
/// One kit, one scoreboard. Sizes are couch-feet so a 1280 Linux player
/// still shows people, ads, and digits — not a green plane and four boxes.
/// </summary>
public static class HarborPostcard
{
    public const string ParkId = "harbor-diamond";

    public const float WallHeightFt = 26f;
    public const float WallThickFt = 3.4f;
    public const float WallPanelWidthFt = 22f;
    public const float AdHeightFt = 12f;
    public const float AdWidthFt = 16f;
    public const float CrowdPersonFt = 12f;
    public const float CrowdInsideFt = 18f;
    public const float TownPastFenceFt = 48f;
    public const float TownHeightFt = 48f;
    public const float DigitHeightFt = 12f;
    public const float ScoreboardPastFenceFt = 22f;

    /// <summary>Seven-seg bits A..G. Same masks HarborKit paints on the park board.</summary>
    public static readonly int[] DigitMask = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

    public static bool Owns(string? parkId) =>
        parkId != null && parkId.Equals(ParkId, StringComparison.OrdinalIgnoreCase);

    public static bool SegOn(int value, int bit)
    {
        value = Math.Clamp(value, 0, 9);
        return (DigitMask[value] & bit) != 0;
    }

    public static double SubtendDeg(double camZ, double objZ, double heightFt)
    {
        var dist = Math.Abs(objZ - camZ);
        if (dist < 1) return 90;
        return Math.Atan(heightFt / dist) * (180.0 / Math.PI);
    }

    /// <summary>
    /// Field pick looks at CF. Wall, track crowd, and digits must subtend
    /// enough angle that they are not specks on a 1280 player.
    /// </summary>
    public static bool ReadsFromField(CameraShot field, double centerFenceFt)
    {
        if (field.Pos.Z < 20 || field.Target.Z < 250) return false;
        var wallZ = centerFenceFt;
        var crowdZ = centerFenceFt - CrowdInsideFt;
        var boardZ = centerFenceFt + ScoreboardPastFenceFt;
        if (SubtendDeg(field.Pos.Z, wallZ, WallHeightFt) < 4) return false;
        if (SubtendDeg(field.Pos.Z, crowdZ, CrowdPersonFt) < 2) return false;
        if (SubtendDeg(field.Pos.Z, boardZ, DigitHeightFt) < 1.5) return false;
        if (centerFenceFt + TownPastFenceFt <= wallZ) return false;
        return true;
    }
}
