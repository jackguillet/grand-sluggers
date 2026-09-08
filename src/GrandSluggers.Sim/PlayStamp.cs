namespace GrandSluggers.Sim;

/// <summary>
/// Big end-of-play callout. Only after the play is dead — not while runners
/// still own the bags. Copy lives here so Unity and tests share one string.
/// </summary>
public static class PlayStamp
{
    public static bool Shows(PlayKind kind) => kind is
        PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Strikeout
        or PlayKind.CaughtStealing
        or PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun;

    public static string Label(PlayKind kind, int outsThisPlay, int runs)
    {
        if (outsThisPlay >= 3) return "TRIPLE PLAY";
        if (outsThisPlay >= 2) return "DOUBLE PLAY";
        if (kind == PlayKind.HomeRun && runs >= 4) return "GRAND SLAM";
        return kind switch
        {
            PlayKind.HomeRun => "HOME RUN",
            PlayKind.Triple => "TRIPLE",
            PlayKind.Double => "DOUBLE",
            PlayKind.Single => "SINGLE",
            PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Strikeout
                or PlayKind.CaughtStealing => "OUT",
            _ => BroadcastHud.Headline(kind)
        };
    }

    /// <summary>
    /// Outs this play. Inning flip zeros <see cref="Match.Outs"/>, so snapshot before Finish.
    /// </summary>
    public static int OutsRecorded(int outsBefore, int inningBefore, bool topBefore, Match after)
    {
        if (after == null) return 0;
        if (after.Inning != inningBefore || after.Top != topBefore)
            return Math.Max(1, 3 - outsBefore);
        return Math.Max(0, after.Outs - outsBefore);
    }

    public static double HoldSeconds(PlayKind kind, FeelTable feel)
    {
        var beat = feel != null ? feel.AfterOutSeconds : 1.35;
        return kind is PlayKind.HomeRun ? Math.Max(2.4, beat) : beat;
    }
}
