namespace GrandSluggers.Sim;

/// <summary>
/// Catcher gun on a steal. One throw to the bag — not a sim roll, not a rundown.
/// Reuses <see cref="FieldAssist.CoverKey"/> and <see cref="InPlay.DiamondBag"/>. Camera is <see cref="PlayCamera.Beat.StealThrow"/>.
/// </summary>
public static class StealThrow
{
    /// <summary>Named bag to arm. Steal target (2 or 3). Never home.</summary>
    public static int DefaultBag(int stealTarget) => stealTarget is 2 or 3 ? stealTarget : 0;

    /// <summary>
    /// Named bag wins. Dead stick arms the steal target (2B on a steal of second).
    /// Home is never the default.
    /// </summary>
    public static int CommitBag(int armed, int stealTarget)
    {
        if (armed is >= 1 and <= 4) return armed;
        return DefaultBag(stealTarget);
    }

    public static string CoverPos(int bag) => FieldAssist.CoverKey(bag is 2 or 3 ? bag : 0);

    public static (double X, double Z) CatcherSpot => Diamond.Positions["C"];

    public static double GunDistFt(int bag)
    {
        var c = CatcherSpot;
        var dest = Diamond.Bag(bag is 2 or 3 ? bag : 2);
        return Diamond.Dist(c.X, c.Z, dest.X, dest.Z);
    }

    /// <summary>Catcher gun to the steal bag: the one throw clock (§8.5, §11.3) over the plate-to-bag distance.</summary>
    public static double GunSec(int bag, ThrowResult? thr, RulesTable? rules = null)
        => CatcherThrowSec(bag is 2 or 3 ? bag : 2, thr, rules);

    /// <summary>Flight from the catcher to a named occupied or steal bag: <see cref="InPlay.ThrowSec"/> with the catcher's arm in <paramref name="thr"/>.</summary>
    public static double CatcherThrowSec(int bag, ThrowResult? thr, RulesTable? rules = null)
    {
        if (bag is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(bag), "Catcher throws need an occupied or steal bag.");
        return InPlay.ThrowSec(GunDistFt(bag), thr, rules);
    }

    /// <summary>
    /// Time from the catch until the runner reaches the steal bag (running.steal). From the bag
    /// (D1, no lead credit): the jump is the break on the pitch. D2 makes it a break at release (P6).
    /// </summary>
    public static double RunnerRemainSec(Character runner, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var bag = RunnerSystem.BagSec(runner, r);
        return Math.Max(r.Running.Steal.RemainMinSec, bag - r.Running.Steal.JumpBaseSec);
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

    /// <summary>A throw to the steal bag is out only when it beats the runner.</summary>
    public static bool OutAtBag(int throwBag, int stealTarget, double releaseSec, double gunSec, double runnerRemain)
    {
        if (throwBag != stealTarget || stealTarget is not 2 and not 3) return false;
        return releaseSec + gunSec < runnerRemain;
    }

    public static bool PlayerOut(
        int throwBag,
        int stealTarget,
        double releaseSec,
        ThrowResult? thr,
        Character runner,
        RulesTable? rules = null)
    {
        var gun = GunSec(stealTarget, thr, rules);
        var remain = RunnerRemainSec(runner, rules);
        return OutAtBag(throwBag, stealTarget, releaseSec, gun, remain);
    }

    /// <summary>Throwing back to the runner's occupied bag is a live pickoff attempt.</summary>
    public static bool PickoffOut(
        int throwBag,
        double releaseSec,
        ThrowResult? thr,
        Character runner,
        RulesTable? rules = null)
    {
        if (throwBag is not 1 and not 2) return false;
        var gun = CatcherThrowSec(throwBag, thr, rules);
        var returnTime = RunnerReturnSec(runner, rules);
        return releaseSec + gun < returnTime;
    }

    /// <summary>
    /// Time from the catch until an armed runner who broke gets back to the bag. A lower Run
    /// rating takes longer to recover; this is the other side of the pickoff race.
    /// </summary>
    public static double RunnerReturnSec(Character runner, RulesTable? rules = null)
    {
        var s = Rules.Or(rules).Running.Steal;
        return Math.Max(s.ReturnMinSec, s.ReturnBaseSec + (10 - runner.Stats.Run) * s.ReturnPerRunDeficitSec);
    }

    public static bool CpuOut(
        Character runner,
        Character catcher,
        int stealTarget,
        ThrowResult? thr,
        Random rng,
        RulesTable? rules = null)
    {
        var release = CpuReleaseSec(catcher, rng, rules);
        var gun = GunSec(stealTarget, thr, rules);
        var remain = RunnerRemainSec(runner, rules)
            + (rng.NextDouble() - 0.5) * Rules.Or(rules).Fielding.Catcher.CpuRemainNoiseSec;
        return OutAtBag(stealTarget, stealTarget, release, gun, remain);
    }
}
