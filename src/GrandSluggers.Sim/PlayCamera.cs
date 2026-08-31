namespace GrandSluggers.Sim;

/// <summary>
/// One named shot per play. 1P and 2P use the same picture (#325).
/// Exhibition SET is <see cref="AtBatShots.Plate"/> for pitch and swing.
/// Training pitching may still stand behind the rubber.
/// </summary>
public static class PlayCamera
{
    public enum Beat
    {
        Set,
        PitchFlight,
        Grounder,
        GrounderPull,
        Line,
        Fly,
        Homer,
        Wall,
        Throw,
        Tag,
        Smash,
        StealThrow,
    }

    public const string Wall = "wall";

    /// <summary>
    /// Seat count must not change the shot. Pass it so 1v1 cannot invent a second rig.
    /// </summary>
    public static string Shot(Beat beat, int seats = 1, bool trainingPitchingSet = false)
    {
        _ = seats;
        return beat switch
        {
            Beat.Set => trainingPitchingSet ? AtBatShots.Mound : AtBatShots.Plate,
            Beat.PitchFlight => AtBatShots.Pitch,
            Beat.Grounder => "diamond-grounder",
            Beat.GrounderPull => "diamond-pull",
            Beat.Line => "diamond-line",
            Beat.Fly => "diamond",
            Beat.Homer => "diamond-homer",
            Beat.Wall => Wall,
            Beat.Throw or Beat.StealThrow => "throw",
            Beat.Tag => "tag",
            Beat.Smash => "smash",
            _ => AtBatShots.Plate
        };
    }

    public static Beat BeatFrom(AtBatResult hit)
    {
        if (!string.IsNullOrEmpty(hit.StarSwingUsed)) return Beat.Smash;
        if (hit.HomeRun) return Beat.Homer;
        if (FieldingResolver.IsGrounder(hit))
            return hit.SprayDeg < -8 ? Beat.GrounderPull : Beat.Grounder;
        if (FieldingResolver.IsLine(hit)) return Beat.Line;
        return Beat.Fly;
    }

    public static string FromHit(AtBatResult hit) => Shot(BeatFrom(hit));
}
