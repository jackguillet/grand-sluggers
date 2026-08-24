namespace GrandSluggers.Sim;

/// <summary>
/// SET / pitch-flight camera. Catcher-eye until the pitcher takes the rubber.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";

    public static string SetShot(bool humanPitches, bool flight, double charge, double aimX, double aimY)
    {
        if (!humanPitches) return Plate;
        if (flight) return Mound;
        if (charge > 0.04) return Mound;
        if (Math.Abs(aimX) + Math.Abs(aimY) > 0.15) return Mound;
        return Plate;
    }
}
