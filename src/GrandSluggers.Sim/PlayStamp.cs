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

    /// <summary>
    /// The stamp from the typed outcome (§15), never from caption text: the outs made name
    /// DOUBLE PLAY / TRIPLE PLAY (a strikeout plus a caught stealing included, S-62); a runner
    /// picked off is PICKED OFF; a sailed throw that let the offense take a bag is ERROR; one out
    /// with the batter safe at first is FIELDER'S CHOICE; the catch feat names BUDDY JUMP / JUMP / DIVE.
    /// </summary>
    public static string Label(PlayEvent ev) =>
        ev == null ? "" : Label(ev.Kind, ev.Outcome?.Outs?.Count ?? ev.OutsOnPlay, ev.RunsScored,
            feat: ev.Outcome?.DefensiveFeat ?? DefensiveFeat.None,
            bunt: ev.Swing.Bunt,
            error: ev.Outcome?.Error ?? false,
            pickedOff: ev.Outcome?.RunnerResult == RunnerPlayResult.PickedOff,
            fieldersChoice: ev.Outcome?.FieldersChoice ?? false);

    /// <param name="feat">The catch feat on the typed outcome (§8.4): BUDDY JUMP, JUMP, DIVE.</param>
    /// <param name="error">A throw sailed and the offense took what it took (§8.5, §8.6): the stamp is ERROR, not the hit.</param>
    /// <param name="pickedOff">The out was a pickoff play (§11.4): PICKED OFF, not CAUGHT STEALING.</param>
    /// <param name="fieldersChoice">One out on another body with the batter safe at first (§10.4).</param>
    public static string Label(PlayKind kind, int outsThisPlay, int runs,
        DefensiveFeat feat = DefensiveFeat.None, bool bunt = false, bool error = false,
        bool pickedOff = false, bool fieldersChoice = false)
    {
        if (outsThisPlay >= 3) return "TRIPLE PLAY";
        if (outsThisPlay >= 2) return "DOUBLE PLAY";
        if (kind == PlayKind.HomeRun && runs >= 4) return "GRAND SLAM";
        if (error && kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.StolenBase) return Error;
        if (pickedOff && kind == PlayKind.CaughtStealing) return "PICKED OFF";
        if (fieldersChoice && outsThisPlay == 1 && kind is PlayKind.GroundOut or PlayKind.FlyOut) return FieldersChoice;
        if (feat == DefensiveFeat.BuddyJump && kind == PlayKind.FlyOut) return "BUDDY JUMP";
        if (bunt && kind is PlayKind.GroundOut or PlayKind.Single or PlayKind.FlyOut)
            return "BUNT";
        if (feat is DefensiveFeat.Jump or DefensiveFeat.SuperJump or DefensiveFeat.Clamber && kind == PlayKind.FlyOut) return "JUMP";
        if (feat == DefensiveFeat.Dive && kind is PlayKind.GroundOut or PlayKind.FlyOut) return "DIVE";
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
    /// The small mid-play tell a live cue pops (§9.6, §8.6): SAFE on the bang-bang body in ahead of
    /// the ball, ERROR on the throw that skipped past its cover. Empty for every other cue.
    /// </summary>
    public static string LiveTell(LiveEvent cue) => cue switch
    {
        LiveEvent.StampSafe => Safe,
        LiveEvent.ThrowSailed => Error,
        _ => ""
    };

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
        // A batter who is a body on the path at Time (a third out made elsewhere, a runner play) is drawn there, not in the box.
        if (last.Outcome?.BodiesAtTime.Any(b => b.IsRunner && b.Who.Id == who.Id) == true)
            return null;
        return who;
    }

    /// <summary>
    /// The contact word (spec §5.2, #578): only on contact, only from the typed zone. A miss
    /// shows STRIKE through the stamp; the release tell (MAX) never claims a hit.
    /// </summary>
    public static string ContactTell(ContactQuality quality) => quality switch
    {
        ContactQuality.Perfect => "PERFECT",
        ContactQuality.Nice => "NICE",
        ContactQuality.Sour => "SOUR",
        _ => ""
    };

    public static double Scale(PlayKind kind) => IsCount(kind) ? 0.72 : 1.0;

    public static double PopSeconds(PlayKind kind) => IsCount(kind) ? 0.10 : 0.16;

    public const string Safe = "SAFE";
    public const string Error = "ERROR";
    public const string FieldersChoice = "FIELDER'S CHOICE";

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
