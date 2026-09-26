namespace GrandSluggers.Sim;

/// <summary>Postgame clip: one play from the log, not an OBP line.</summary>
public enum HighlightBeat
{
    None,
    ExtraBase,
    HomeRun,
    StarK,
    RobbedHomer,
    BuddyJump
}

public sealed record HighlightClip(PlayEvent Play, HighlightBeat Beat, int Score)
{
    /// <summary>The replay follows the ball's flight: a batted ball (fair, foul, caught or not), never a strikeout.</summary>
    public bool UsesFlightPath => Play.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double
        or PlayKind.Single or PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Foul;

    /// <summary>What the replay's camera centers on.</summary>
    public HighlightAim Aim => Beat switch
    {
        HighlightBeat.BuddyJump or HighlightBeat.RobbedHomer => HighlightAim.Moment,
        HighlightBeat.StarK => HighlightAim.Batter,
        _ => HighlightAim.Ball
    };
}

/// <summary>The replay's subject: the ball in flight, the moment the play was made (the catch at the wall), or the batter.</summary>
public enum HighlightAim { Ball, Moment, Batter }

public static class Highlight
{
    public static HighlightClip? Pick(IReadOnlyList<PlayEvent> log)
    {
        HighlightClip? best = null;
        foreach (var ev in log)
        {
            var clip = Grade(ev);
            if (clip.Beat == HighlightBeat.None) continue;
            if (best is null || clip.Score >= best.Score)
                best = clip;
        }
        return best;
    }

    public static HighlightBeat BeatOf(PlayEvent ev) => Grade(ev).Beat;

    static HighlightClip Grade(PlayEvent ev)
    {
        if (ev.Kind == PlayKind.FlyOut && ev.Outcome?.DefensiveFeat == DefensiveFeat.BuddyJump)
            return new HighlightClip(ev, HighlightBeat.BuddyJump, 100);
        if (ev.Kind == PlayKind.FlyOut && (ev.Outcome?.DefensiveFeat == DefensiveFeat.Clamber
            || ev.Outcome?.DefensiveFeat == DefensiveFeat.Jump && ev.AtBat.HomeRun))
            return new HighlightClip(ev, HighlightBeat.RobbedHomer, 90);
        if (ev.Kind == PlayKind.HomeRun)
            return new HighlightClip(ev, HighlightBeat.HomeRun, 80);
        if (ev.Kind == PlayKind.Strikeout && StarPitch(ev))
            return new HighlightClip(ev, HighlightBeat.StarK, 70);
        if (ev.Kind == PlayKind.Triple)
            return new HighlightClip(ev, HighlightBeat.ExtraBase, 50);
        if (ev.Kind == PlayKind.Double)
            return new HighlightClip(ev, HighlightBeat.ExtraBase, 40);
        if (ev.Kind == PlayKind.Single)
            return new HighlightClip(ev, HighlightBeat.ExtraBase, 10);
        return new HighlightClip(ev, HighlightBeat.None, 0);
    }

    static bool StarPitch(PlayEvent ev) =>
        ev.Pitch.Star || !string.IsNullOrEmpty(ev.AtBat.StarPitchUsed);

}
