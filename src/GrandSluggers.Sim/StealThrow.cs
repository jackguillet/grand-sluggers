namespace GrandSluggers.Sim;

/// <summary>
/// Catcher gun on a steal. One throw to the bag — not a sim roll, not a rundown.
/// Reuses <see cref="FieldAssist.CoverKey"/> / <see cref="FieldAssist.AfterThrowPos"/>
/// and <see cref="InPlay.DiamondBag"/>. Camera is <see cref="PlayCamera.Beat.StealThrow"/>.
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

    public static string AfterThrowPos(string currentPos, int bag) =>
        FieldAssist.AfterThrowPos(currentPos, bag);

    public static (double X, double Z) CatcherSpot => Diamond.Positions["C"];

    public static double GunDistFt(int bag)
    {
        var c = CatcherSpot;
        var dest = Diamond.Bag(bag is 2 or 3 ? bag : 2);
        return Diamond.Dist(c.X, c.Z, dest.X, dest.Z);
    }

    /// <summary>Catcher pop, release to tag. Faster than a hopper relay.</summary>
    public static double GunSec(int bag, ThrowResult? thr, RulesTable? rules = null)
        => CatcherThrowSec(bag is 2 or 3 ? bag : 2, thr, rules);

    /// <summary>Flight from the catcher to a named occupied or steal bag (fielding.catcher).</summary>
    public static double CatcherThrowSec(int bag, ThrowResult? thr, RulesTable? rules = null)
    {
        if (bag is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(bag), "Catcher throws need an occupied or steal bag.");
        var k = Rules.Or(rules).Fielding.Catcher;
        var mul = thr?.SpeedMul ?? 1;
        if (thr is { Error: true }) mul *= k.ErrorMul;
        var fps = k.BaseFtPerSec * Math.Max(k.MinMul, mul);
        var c = CatcherSpot;
        var dest = Diamond.Bag(bag);
        var dist = Diamond.Dist(c.X, c.Z, dest.X, dest.Z);
        return k.ReleaseSec + dist / Math.Max(k.MinFtPerSec, fps);
    }

    /// <summary>
    /// Time from the catch until the runner reaches the steal bag (running.steal).
    /// Lead is a jump; they have been going since first move.
    /// </summary>
    public static double RunnerRemainSec(Character runner, double lead01, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var bag = InPlay.BagToBagSec(runner, r);
        var jump = r.Running.Steal.JumpBaseSec + Math.Clamp(lead01, 0, 1) * r.Running.Steal.JumpPerLeadSec;
        return Math.Max(r.Running.Steal.RemainMinSec, bag - jump);
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
        double lead01,
        RulesTable? rules = null)
    {
        var gun = GunSec(stealTarget, thr, rules);
        var remain = RunnerRemainSec(runner, lead01, rules);
        return OutAtBag(throwBag, stealTarget, releaseSec, gun, remain);
    }

    /// <summary>Throwing back to the runner's occupied bag is a live pickoff attempt.</summary>
    public static bool PickoffOut(
        int throwBag,
        double releaseSec,
        ThrowResult? thr,
        Character runner,
        double lead01,
        RulesTable? rules = null)
    {
        if (throwBag is not 1 and not 2) return false;
        var gun = CatcherThrowSec(throwBag, thr, rules);
        var returnTime = RunnerReturnSec(runner, lead01, rules);
        return releaseSec + gun < returnTime;
    }

    /// <summary>
    /// Time from the catch until a runner reaches the occupied bag. A larger lead and lower
    /// Run rating both take longer to recover; this is the other side of the pickoff race.
    /// </summary>
    public static double RunnerReturnSec(Character runner, double lead01, RulesTable? rules = null)
    {
        var s = Rules.Or(rules).Running.Steal;
        return Math.Max(s.ReturnMinSec, s.ReturnBaseSec + Math.Clamp(lead01, 0, 1) * s.ReturnPerLeadSec
            + (10 - runner.Stats.Run) * s.ReturnPerRunDeficitSec);
    }

    public static bool CpuOut(
        Character runner,
        Character catcher,
        double lead01,
        int stealTarget,
        ThrowResult? thr,
        Random rng,
        RulesTable? rules = null)
    {
        var release = CpuReleaseSec(catcher, rng, rules);
        var gun = GunSec(stealTarget, thr, rules);
        var remain = RunnerRemainSec(runner, lead01, rules)
            + (rng.NextDouble() - 0.5) * Rules.Or(rules).Fielding.Catcher.CpuRemainNoiseSec;
        return OutAtBag(stealTarget, stealTarget, release, gun, remain);
    }
}
