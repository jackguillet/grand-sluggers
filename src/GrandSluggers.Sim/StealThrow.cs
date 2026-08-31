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
    public static double GunSec(int bag, ThrowResult? thr)
    {
        var mul = thr?.SpeedMul ?? 1;
        if (thr is { Error: true }) mul *= 0.72;
        var fps = 96 * Math.Max(0.45, mul);
        return 0.12 + GunDistFt(bag) / Math.Max(64, fps);
    }

    /// <summary>
    /// Time from the catch until the runner reaches the steal bag.
    /// Lead is a jump; they have been going since first move.
    /// </summary>
    public static double RunnerRemainSec(Character runner, double lead01)
    {
        var bag = InPlay.BagToBagSec(runner);
        var jump = 0.62 + Math.Clamp(lead01, 0, 1) * 1.08;
        return Math.Max(0.58, bag - jump);
    }

    /// <summary>CPU catcher release. Dead stick still guns.</summary>
    public static double CpuReleaseSec(Character catcher, Random rng)
    {
        var hands = Math.Clamp(catcher.Stats.Field, 1, 10);
        var mean = 0.42 - hands * 0.014;
        var noise = (rng.NextDouble() - 0.5) * 0.20;
        return Math.Clamp(mean + noise, 0.10, 0.58);
    }

    /// <summary>Wrong bag is always safe. Early throw that beats the remain is out.</summary>
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
        double lead01)
    {
        var gun = GunSec(stealTarget, thr);
        var remain = RunnerRemainSec(runner, lead01);
        return OutAtBag(throwBag, stealTarget, releaseSec, gun, remain);
    }

    public static bool CpuOut(
        Character runner,
        Character catcher,
        double lead01,
        int stealTarget,
        ThrowResult? thr,
        Random rng)
    {
        var release = CpuReleaseSec(catcher, rng);
        var gun = GunSec(stealTarget, thr);
        var remain = RunnerRemainSec(runner, lead01) + (rng.NextDouble() - 0.5) * 0.28;
        return OutAtBag(stealTarget, stealTarget, release, gun, remain);
    }
}
