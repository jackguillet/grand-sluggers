namespace GrandSluggers.Sim;

/// <summary>
/// SET / pitch-flight camera. Batting SET is over-the-batter (plate + chalk).
/// When they throw at you, look at the pitcher. Pitching SET is 3/4 over the rubber.
/// Catcher-spine is not a SET shot.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";
    public const string Pitch = "pitch";

    public static string SetShot(bool humanPitches, bool flight, double charge, double aimX, double aimY)
    {
        _ = charge;
        _ = aimX;
        _ = aimY;
        if (humanPitches) return Mound;
        return flight ? Pitch : Plate;
    }
}
