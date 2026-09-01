namespace GrandSluggers.Sim;

/// <summary>
/// SET / throw camera ids. 1P is mound (behind the pitcher). 1v1 is plate
/// (behind home). Catcher-spine is not a SET shot. <c>pitch</c> stays a still-gate id.
/// </summary>
public static class AtBatShots
{
    public const string Plate = "plate";
    public const string Mound = "mound";
    public const string Pitch = "pitch";

    /// <param name="seats">1 = behind the pitcher. 2 = behind home. Role is ignored.</param>
    public static string SetShot(
        bool humanPitches, bool flight, double charge, double aimX, double aimY,
        bool training = false, int seats = 1)
    {
        _ = humanPitches;
        _ = charge;
        _ = aimX;
        _ = aimY;
        _ = training;
        var beat = flight ? PlayCamera.Beat.PitchFlight : PlayCamera.Beat.Set;
        return PlayCamera.Shot(beat, seats);
    }
}
