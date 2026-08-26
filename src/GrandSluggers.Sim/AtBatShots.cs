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
        // Flight is the throw: look at the pitcher so the ball leaves that hand toward the plate.
        // SET pitching stays mound; SET batting stays plate. Do not keep mound through a human pitch —
        // that camera looks CF and the throw is behind the lens (#301).
        if (flight) return Pitch;
        return humanPitches ? Mound : Plate;
    }
}
