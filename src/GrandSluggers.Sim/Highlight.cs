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

public sealed record HighlightClip(PlayEvent Play, HighlightBeat Beat, int Score);

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
        var cap = ev.Caption ?? "";
        if (ev.Kind == PlayKind.FlyOut && Has(cap, "BUDDY"))
            return new HighlightClip(ev, HighlightBeat.BuddyJump, 100);
        if (ev.Kind == PlayKind.FlyOut && (Has(cap, "SUPER JUMP") || Has(cap, "CLAMBER")))
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

    static bool Has(string caption, string token) =>
        caption.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
}
