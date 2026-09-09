namespace GrandSluggers.Sim;

/// <summary>
/// End-of-play callout. Only after the play is dead — not while runners
/// still own the bags. Copy lives here so Unity and tests share one string.
/// Counts (ball / strike / foul / walk) are smaller and quicker than outs and hits.
/// </summary>
public static class PlayStamp
{
    public static bool IsCount(PlayKind kind) => kind is
        PlayKind.TakeBall or PlayKind.TakeStrike or PlayKind.SwingMiss
        or PlayKind.Foul or PlayKind.Walk;

    public static bool Shows(PlayKind kind) => IsCount(kind) || kind is
        PlayKind.FlyOut or PlayKind.GroundOut or PlayKind.Strikeout
        or PlayKind.HitByPitch
        or PlayKind.CaughtStealing or PlayKind.StolenBase
        or PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun;

    public static string Label(PlayEvent ev, int outsThisPlay) =>
        ev == null ? "" : Label(ev.Kind, outsThisPlay, ev.RunsScored, ev.Swing.Bunt);

    public static string Label(PlayKind kind, int outsThisPlay, int runs,
        bool bunt = false, bool dive = false, bool jump = false)
    {
        if (outsThisPlay >= 3) return "TRIPLE PLAY";
        if (outsThisPlay >= 2) return "DOUBLE PLAY";
        if (kind == PlayKind.HomeRun && runs >= 4) return "GRAND SLAM";
        if (bunt && kind is PlayKind.GroundOut or PlayKind.Single or PlayKind.FlyOut)
            return "BUNT";
        if (jump && kind == PlayKind.FlyOut) return "JUMP";
        if (dive && kind is PlayKind.GroundOut or PlayKind.FlyOut) return "DIVE";
        return kind switch
        {
            PlayKind.HomeRun => "HOME RUN",
            PlayKind.Triple => "TRIPLE",
            PlayKind.Double => "DOUBLE",
            PlayKind.Single => "SINGLE",
            PlayKind.TakeBall => "BALL",
            PlayKind.TakeStrike or PlayKind.SwingMiss => "STRIKE",
            PlayKind.Foul => "FOUL",
            PlayKind.Walk => "WALK",
            PlayKind.HitByPitch => "HIT BY PITCH",
            PlayKind.Strikeout => "STRIKE OUT",
            PlayKind.StolenBase => "STOLEN BASE",
            PlayKind.CaughtStealing => "CAUGHT STEALING",
            PlayKind.FlyOut or PlayKind.GroundOut => "OUT",
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

    /// <summary>
    /// Body in the box during the result stamp. Null if that batter is already
    /// a runner. The next batter waits for SET.
    /// </summary>
    public static Character? BoxBatter(PlayEvent? last, Match? match)
    {
        if (last?.Batter == null) return match?.Batter;
        var who = last.Batter;
        if (match != null && (match.First == who || match.Second == who || match.Third == who))
            return null;
        return who;
    }

    public static double Scale(PlayKind kind) => IsCount(kind) ? 0.72 : 1.0;

    public static double PopSeconds(PlayKind kind) => IsCount(kind) ? 0.10 : 0.16;

    public const string Safe = "SAFE";

    /// <summary>
    /// Hits and outs stamp on the live field camera. Counts stay on SET.
    /// Next pitch SET is after the hold, not at the stamp (#301).
    /// </summary>
    public static bool HoldsLiveCamera(PlayKind kind) => Shows(kind) && !IsCount(kind);

    public static double SafeScale => 0.72;

    public static double SafePopSeconds => 0.10;

    public static double SafeHoldSeconds(FeelTable feel) =>
        feel != null ? feel.AfterCountSeconds : 0.7;

    public static double HoldSeconds(PlayKind kind, FeelTable feel)
    {
        if (IsCount(kind) || kind == PlayKind.HitByPitch)
            return feel != null ? feel.AfterCountSeconds : 0.7;
        var beat = feel != null ? feel.AfterOutSeconds : 1.35;
        return kind is PlayKind.HomeRun ? Math.Max(2.4, beat) : beat;
    }
}
