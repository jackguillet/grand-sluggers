namespace GrandSluggers.Sim;

/// <summary>
/// The catcher's side of a steal (spec §11.3): where the catcher stands with the ball at the
/// crossing, who covers each bag, and the CPU release. The throw itself is the one throw model
/// (<see cref="InPlay.ThrowSec"/>) and the out is the tag at the bag (§10.3) — nothing here decides
/// a runner. Camera is <see cref="PlayCamera.Beat.StealThrow"/>.
/// </summary>
public static class StealThrow
{
    /// <summary>The cover of a bag on a steal or a pickoff: 1B, SS / 2B, 3B, and the catcher at home (§11.4).</summary>
    public static string CoverPos(int bag) => FieldAssist.CoverKey(bag);

    /// <summary>The catcher with the ball at the crossing (§11.3): fielding.catcher.behindPlateFt behind the plate. The SET crouch (<see cref="Diamond.Positions"/>) is presentation.</summary>
    public static (double X, double Z) CatcherSpot(RulesTable? rules = null) =>
        (Diamond.Home.X, Diamond.Home.Z - Rules.Or(rules).Fielding.Catcher.BehindPlateFt);

    /// <summary>Flight from the catcher to a named bag: <see cref="InPlay.ThrowSec"/> with the catcher's arm in <paramref name="thr"/>.</summary>
    public static double CatcherThrowSec(int bag, ThrowResult? thr, RulesTable? rules = null)
    {
        if (bag is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(bag), "Catcher throws go to a bag.");
        var c = CatcherSpot(rules);
        var dest = Diamond.Bag(bag);
        return InPlay.ThrowSec(Diamond.Dist(c.X, c.Z, dest.X, dest.Z), thr, rules);
    }

    /// <summary>CPU catcher release (fielding.catcher.cpuRelease*, × cpu reactionMul). Dead stick still guns.</summary>
    public static double CpuReleaseSec(Character catcher, Random rng, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var k = r.Fielding.Catcher;
        var hands = Math.Clamp(catcher.Stats.Field, 1, 10);
        var mean = k.CpuReleaseBaseSec - hands * k.CpuReleasePerField;
        var noise = (rng.NextDouble() - 0.5) * k.CpuReleaseNoiseSec;
        return Math.Clamp(mean + noise, k.CpuReleaseMinSec, k.CpuReleaseMaxSec) * r.Cpu.Active.ReactionMul;
    }
}
