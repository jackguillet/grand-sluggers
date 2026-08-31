namespace GrandSluggers.Sim;

/// <summary>
/// SET / pitch-flight camera ids. Exhibition SET is always plate (#325).
/// Training pitching may still use mound. Catcher-spine is not a SET shot.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";
    public const string Pitch = "pitch";

    /// <param name="training">
    /// Training pitching lesson may stand behind the rubber. Exhibition (and 1v1)
    /// always uses plate so 1P pitching equals 2P pitching.
    /// </param>
    public static string SetShot(
        bool humanPitches, bool flight, double charge, double aimX, double aimY,
        bool training = false, int seats = 1)
    {
        _ = charge;
        _ = aimX;
        _ = aimY;
        if (flight) return PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats);
        return PlayCamera.Shot(PlayCamera.Beat.Set, seats, training && humanPitches);
    }
}
