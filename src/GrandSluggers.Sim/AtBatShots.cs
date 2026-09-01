namespace GrandSluggers.Sim;

/// <summary>
/// SET / pitch-flight camera ids. Role, not pad count: pitcher view is mound,
/// batter view is plate. Flight is always pitch. Catcher-spine is not a SET shot.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";
    public const string Pitch = "pitch";

    /// <param name="humanPitches">
    /// Pitcher view when a human is on the rubber (Exhibition top, 1v1, Training
    /// pitching). Batter view otherwise. Seats are ignored.
    /// </param>
    public static string SetShot(
        bool humanPitches, bool flight, double charge, double aimX, double aimY,
        bool training = false, int seats = 1)
    {
        _ = charge;
        _ = aimX;
        _ = aimY;
        _ = training;
        if (flight) return PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats);
        return PlayCamera.Shot(PlayCamera.Beat.Set, seats, pitchingSet: humanPitches);
    }
}
