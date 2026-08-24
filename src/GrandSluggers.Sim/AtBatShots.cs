namespace GrandSluggers.Sim;

/// <summary>
/// SET / pitch-flight camera. Batting is over-the-batter looking at the mound
/// (plate + chalk boxes read). Pitching is 3/4 over-the-pitcher looking at that box.
/// Catcher-spine is not a SET shot.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";

    public static string SetShot(bool humanPitches, bool flight, double charge, double aimX, double aimY)
    {
        _ = flight;
        _ = charge;
        _ = aimX;
        _ = aimY;
        return humanPitches ? Mound : Plate;
    }
}
