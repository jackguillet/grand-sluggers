namespace GrandSluggers.Sim;

/// <summary>
/// SMS close play: when a throw and a runner arrive together at third or home,
/// the camera sits on the bag and the first South wins. Offense safe, defense out.
/// </summary>
public static class ClosePlay
{
    /// <summary>Seconds after the throw lands until the mash icon appears (running.close.iconDelaySec).</summary>
    public static double IconDelaySec(RulesTable rules) => rules.Running.Close.IconDelaySec;

    /// <summary>Mash at third/home on a tag. A force is the throw, not a mash; nobody coming is no play.</summary>
    public static bool Offered(int throwBag, InPlay.ForceState force, bool runnerHeadingThere)
    {
        if (throwBag is not (3 or 4)) return false;
        if (force.At(throwBag)) return false;
        return runnerHeadingThere;
    }

    public static bool IsCloseBag(int bag) => bag is 3 or 4;

    /// <summary>
    /// The geometric gate (§9.6, D5): the ball is on the bag now and the body arrives in
    /// <paramref name="runnerArrivalSec"/>; the mash runs only when that is inside running.close.marginSec.
    /// A body already there is safe (§10.3); one further out is tagged or not by the reach.
    /// </summary>
    public static bool WithinMargin(double runnerArrivalSec, RulesTable rules) =>
        runnerArrivalSec > 0 && runnerArrivalSec <= rules.Running.Close.MarginSec;

    /// <summary>Seconds after the icon until a CPU side mashes (running.close, × cpu reactionMul). Better Hands (defense: the tag) or Run (offense) is faster.</summary>
    public static double CpuReactionSec(int stat, RulesTable rules)
    {
        var r = rules;
        var n = Math.Clamp(stat, 1, 10);
        return (r.Running.Close.CpuReactionBaseSec + (10 - n) * r.Running.Close.CpuReactionPerStat) * r.Cpu.Active.ReactionMul;
    }

    /// <summary>First press after the icon wins. A missing press is never. Tie goes to the runner.</summary>
    public static bool OffenseSafe(double offenseAt, double defenseAt) =>
        offenseAt <= defenseAt;

    public static string Caption(int bag, bool safe) =>
        safe
            ? (bag == 4 ? "SAFE at home!" : "SAFE at third!")
            : (bag == 4 ? "OUT at home!" : "OUT at third!");
}
