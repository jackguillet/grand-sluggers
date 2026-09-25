namespace GrandSluggers.Sim;

/// <summary>
/// A motion style (CH-12, CF-5): a body class's own takes of the styled clips (run, walk, idle, the batting stance in the
/// two swings, the windup in the two pitches, and cheer, its captain's signature beat), baked from the one takes script
/// into <c>{styles slot}/{Id}/{clip}</c>. Every other clip is the shared take, unless <see cref="Reach"/> moves the
/// joints: then the style owns every clip. Rows: <c>data/art/clips.json</c> <c>styles</c>.
/// </summary>
/// <param name="Signature">The captain's signature beat: the style's cheer take (home run, the win).</param>
/// <param name="RunCycle">Rig units of ground per run loop (two steps); <see cref="Gait"/> turns it into feet for a body.</param>
/// <param name="WalkCycle">Rig units of ground per walk loop.</param>
/// <param name="Reach">Arm length (<see cref="Silhouette.ReachBuild"/>); 1 is the shared arm.</param>
/// <param name="Boots">Shoe size (<see cref="Silhouette.BootsBuild"/>); 1 is the shared shoe.</param>
public sealed record MotionStyle(
    string Id, string Signature, double RunCycle, double WalkCycle, double Reach, double Boots,
    IReadOnlyCollection<string> Clips)
{
    /// <summary>A style whose reach moves the elbow and wrist bakes every clip; the shared takes would not reach its hands.</summary>
    public bool OwnsEveryClip => Math.Abs(Reach - 1) > 1e-9;

    /// <summary>Whether this style has its own take of <paramref name="clipId"/>.</summary>
    public bool Owns(string clipId) => Clips.Contains(clipId, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Locomotion presentation on the sim clock (CH-12, SC-21, #1111): which loop a moving body plays and how far through it
/// the body is. The walk / run choice comes from the body's own pursuit profile, and the loop's phase advances with the
/// ground the body covers, so stride rate follows ground speed for every body at every speed.
/// </summary>
public static class Gait
{
    /// <summary>Rig units of ground per loop of the shared run and walk takes (a body with no style).</summary>
    public const double SharedRunCycle = 6.8, SharedWalkCycle = 3.4;

    /// <summary>
    /// The body's slowest full-effort pursuit speed (§8.1): its one glove speed × the smallest hit-class multiplier
    /// (<c>fielding.chase.outfieldAirMul</c> / <c>infieldAirMul</c>). A fly chase at full speed is at least this fast.
    /// </summary>
    public static double SlowestPursuitFt(Character who, RulesTable rules)
    {
        var c = rules.Fielding.Chase;
        return FieldingResolver.ChaseSpeedFt(who, false, rules) * Math.Min(1, Math.Min(c.OutfieldAirMul, c.InfieldAirMul));
    }

    /// <summary>Above this ground speed the body plays the run take: <paramref name="runOfPursuit"/> of <see cref="SlowestPursuitFt"/>.</summary>
    public static double RunFloorFt(Character who, RulesTable rules, double runOfPursuit) =>
        runOfPursuit * SlowestPursuitFt(who, rules);

    /// <summary>
    /// The locomotion take for a body moving at <paramref name="speedFt"/>: run at or above its run floor, walk above
    /// <see cref="CartoonJuice.WalkFtPerSec"/>, otherwise <paramref name="still"/>. The backpedal (§8.2) is always the walk.
    /// </summary>
    public static Motion.Verb Locomotion(double speedFt, double runFloorFt, bool backpedal, Motion.Verb still)
    {
        if (speedFt <= CartoonJuice.WalkFtPerSec) return still;
        if (backpedal) return Motion.Verb.Walk;
        return speedFt >= runFloorFt ? Motion.Verb.Run : Motion.Verb.Walk;
    }

    /// <summary>Feet of ground per loop for a body: the style's rig-unit cycle × the body's root height scale.</summary>
    public static double CycleFt(double cycleRig, Silhouette.Spec spec) =>
        cycleRig * Silhouette.SharedRootScale(spec).Y;

    /// <summary>The loop's phase 0..1 after <paramref name="distanceFt"/> more ground: distance, not time, turns the legs.</summary>
    public static double Advance(double phase, double distanceFt, double cycleFt)
    {
        if (cycleFt <= 0 || distanceFt <= 0) return phase;
        var p = (phase + distanceFt / cycleFt) % 1.0;
        return p < 0 ? p + 1 : p;
    }
}

/// <summary>The walk / run numbers a body reads at presentation time: the rules' pursuit profile and the feel share.</summary>
public sealed class GaitProfile(RulesTable rules, double runOfPursuit)
{
    public double RunOfPursuit { get; } = runOfPursuit;

    public double RunFloorFt(Character who) => Gait.RunFloorFt(who, rules, RunOfPursuit);
}
