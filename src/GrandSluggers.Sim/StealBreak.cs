namespace GrandSluggers.Sim;

/// <summary>When a runner's steal was armed (spec §11.1, §11.2, D2, D3).</summary>
public enum StealArm
{
    None,
    /// <summary>Armed in SET, before the windup: breaks on the pitcher's first motion — a pickoff motion included (D3).</summary>
    Set,
    /// <summary>Armed during the windup, past the perfect window: breaks at release.</summary>
    Windup,
    /// <summary>Armed inside the first running.steal.perfectWindowSec of the windup: breaks perfectEarlySec before release (D2).</summary>
    Perfect
}

/// <summary>
/// The steal jump (spec §11.2, D2): an armed runner breaks at release (a perfect steal
/// <c>perfectEarlySec</c> before it) and runs at <c>airSpeedMul</c> of their speed until the ball
/// reaches the plate, full speed after. There is no time credit: these functions place the body
/// on the clock the pitch flies on, and the tag at the bag decides (§10.3).
/// </summary>
public static class StealBreak
{
    /// <summary>
    /// The arm a press at <paramref name="windupSec"/> seconds into the windup makes: negative (SET) is
    /// the ordinary arm, inside the perfect window is the perfect steal, later in the windup breaks at
    /// release, and a press after release (<see cref="Motion.PitchRelease"/>) is too late — nothing.
    /// </summary>
    public static StealArm ArmFor(double windupSec, RulesTable? rules = null)
    {
        if (double.IsNaN(windupSec) || windupSec < 0) return StealArm.Set;
        if (windupSec <= Rules.Or(rules).Running.Steal.PerfectWindowSec) return StealArm.Perfect;
        if (windupSec < Motion.PitchRelease) return StealArm.Windup;
        return StealArm.None;
    }

    /// <summary>Seconds before release this arm breaks: the perfect steal's head start, 0 for the rest.</summary>
    public static double BreakBeforeReleaseSec(StealArm arm, RulesTable? rules = null) =>
        arm == StealArm.Perfect ? Rules.Or(rules).Running.Steal.PerfectEarlySec : 0;

    /// <summary>Feet off the bag <paramref name="sinceReleaseSec"/> after release, while the pitch is in the air (the ⅔ run).</summary>
    public static double FeetAt(Character who, StealArm arm, double sinceReleaseSec, RulesTable? rules = null)
    {
        if (arm == StealArm.None) return 0;
        var r = Rules.Or(rules);
        var running = Math.Max(0, sinceReleaseSec + BreakBeforeReleaseSec(arm, r));
        var feet = RunnerSystem.SpeedFtPerSec(who, 0, r) * r.Running.Steal.AirSpeedMul * running;
        return Math.Min(feet, Diamond.Baseline);
    }

    /// <summary>The body's head start when the ball reaches the plate: the run through the pitch's <paramref name="airSec"/>.</summary>
    public static double HeadStartFt(Character who, StealArm arm, double airSec, RulesTable? rules = null) =>
        FeetAt(who, arm, airSec, rules);

    /// <summary>A pickoff motion in SET is the pitcher's first motion: only a runner armed in SET has broken on it (D3).</summary>
    public static bool BreaksOnPickoff(StealArm arm) => arm == StealArm.Set;
}
