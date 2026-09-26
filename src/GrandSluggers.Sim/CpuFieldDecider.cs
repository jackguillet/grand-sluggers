namespace GrandSluggers.Sim;

/// <summary>What the CPU glove does with the ball once its reaction is spent (§8.8).</summary>
public enum CpuFieldAction
{
    /// <summary>Hold: nobody is out on a throw; Time comes when the bodies settle (§10.6).</summary>
    Hold,
    /// <summary>Carry the ball to <see cref="CpuFieldDecision.Bag"/> and make the play there on foot.</summary>
    WalkTo,
    /// <summary>Throw to <see cref="CpuFieldDecision.Bag"/>, straight or through the cutoff as the plan says (§8.7).</summary>
    ThrowTo,
    /// <summary>Nothing makeable from the outfield: the ball comes in to the cutoff or ahead of the lead runner (§8.8 rule 5).</summary>
    ThrowIn,
}

/// <summary>The CPU glove's decision: an action and, for a walk or a throw, the bag.</summary>
public readonly record struct CpuFieldDecision(CpuFieldAction Action, int Bag = 0)
{
    public static CpuFieldDecision Hold => new(CpuFieldAction.Hold);
}

/// <summary>
/// The live play as the CPU glove's decision table reads it (§8.8): the rules, the clock, the glove, the bodies and the
/// score, and the clock's three reads of getting the ball to a bag. Read-only; the table changes nothing.
/// </summary>
public interface ICpuFieldView
{
    RulesTable Rules { get; }
    double Elapsed { get; }
    double GloveX { get; }
    double GloveZ { get; }
    double Dash01 { get; }
    InPlay.ForceState Forces { get; }
    FlyState Fly { get; }
    IReadOnlyList<Runner> Runners { get; }
    int Outs { get; }
    int HomeScore { get; }
    int AwayScore { get; }
    BattedBall? Ball { get; }
    AtBatResult? Hit { get; }

    /// <summary>The runner starting at <paramref name="fromBag"/>, if any.</summary>
    Runner? RunnerAt(int fromBag);

    /// <summary>The runner a play at <paramref name="bag"/> is on, if any.</summary>
    Runner? RunnerForBag(int bag);

    /// <summary>Seconds until a throw from the glove is at <paramref name="bag"/>, by the leg the CPU would take.</summary>
    double ThrowArrivalSec(int bag);

    /// <summary>Seconds for the glove to carry the ball to <paramref name="bag"/>.</summary>
    double WalkSec(int bag);

    /// <summary>Seconds until a throw to <paramref name="bag"/> is in a glove on it (the flight, or the cover's walk if longer).</summary>
    double ThrowReadySec(int bag);

    /// <summary>
    /// Seconds the glove may still hold the ball before a hot ball drops at its feet (§13, <see cref="HotBall"/>); infinite for
    /// every ordinary ball. A walk that outlasts it is no play on foot.
    /// </summary>
    double HoldLeftSec { get; }
}

/// <summary>
/// The CPU fielder's decision table (§8.8), computed from the live bodies the moment the throw may go: for each bag,
/// <c>margin = runnerArrival − throwArrival</c>; a play is makeable above the difficulty's margin. Infielders: the lead force,
/// home, third, first, else hold (or, on the grass, throw in ahead of the lead runner). Outfielders: home if a run is at stake,
/// third, second, else the cutoff. <see cref="LivePlaySystem"/> carries the decision out.
/// </summary>
public static class CpuFieldDecider
{
    public static CpuFieldDecision Decide(ICpuFieldView v)
    {
        var R = v.Rules;
        var makeable = R.Cpu.Active.MakeableMarginSec;
        var onGrass = FieldingResolver.OutfieldGrass(v.GloveX, v.GloveZ, R);
        double Margin(int bag)
        {
            var runner = v.RunnerForBag(bag);
            if (runner is null || runner.Bag >= bag) return double.NegativeInfinity;
            if (!runner.Forced || runner.FromBag != bag - 1)
            {
                // An unforced runner is a candidate only when their body is bound for the bag.
                if (!(runner.Advancing && runner.DestBag >= bag && runner.Bag == bag - 1)) return double.NegativeInfinity;
            }
            return RunnerSystem.ArrivalSec(runner, bag, v.Elapsed, R, v.Dash01) - PlayArrivalSec(v, bag);
        }
        bool Makeable(int bag) => Margin(bag) > makeable;
        // A tag play is worth the throw (§8.8 rules 2–3, §11.3): the bag is played when the ball can land inside the
        // close margin of the body, even short of the tie band; the mash (third, home) or the tag at the bag decides.
        // A glove never concedes a bag by holding the ball while the race is that close.
        bool TagWorthIt(int bag) => !v.Forces.At(bag) && Margin(bag) > -R.Running.Close.MarginSec;
        bool PlateWorthIt() => TagWorthIt(4);
        double DistTo(int bag)
        {
            var at = DiamondGeometry.Of(v.Rules).Bag(bag);
            return Diamond.Dist(v.GloveX, v.GloveZ, at.X, at.Z);
        }
        // The lead forced bag ahead of a forced runner still short of it (second, third, home), or 0.
        int LeadForce()
        {
            for (var bag = 4; bag >= 2; bag--)
            {
                if (!v.Forces.At(bag)) continue;
                var forced = v.RunnerAt(bag - 1);
                if (forced is null || !forced.Live || forced.Bag >= bag) continue;
                return bag;
            }
            return 0;
        }

        // The doubled-off race (§10.5): a body off its start bag after the catch is a force back there.
        if (v.Fly == FlyState.Caught)
        {
            Runner? best = null;
            var bestMargin = double.NegativeInfinity;
            foreach (var r in v.Runners)
            {
                if (!r.Live || !r.LeftEarly) continue;
                var margin = RunnerSystem.ReturnSec(r, R, v.Dash01) - v.ThrowArrivalSec(r.FromBag);
                if (margin > bestMargin) { bestMargin = margin; best = r; }
            }
            if (best is not null && bestMargin > makeable) return PlayAt(v, best.FromBag);
        }

        if (!onGrass)
        {
            // Two outs (§10.4, S-50): any makeable out ends the inning; the shortest throw among them.
            if (v.Outs >= 2)
            {
                var pick = 0;
                var pickDist = double.MaxValue;
                for (var bag = 1; bag <= 4; bag++)
                {
                    var candidate = bag == 1 ? v.Forces.At(1) : v.Forces.At(bag) || v.RunnerForBag(bag) is not null;
                    if (!candidate || !Makeable(bag)) continue;
                    var d = DistTo(bag);
                    if (d < pickDist) { pickDist = d; pick = bag; }
                }
                return pick > 0 ? PlayAt(v, pick) : CpuFieldDecision.Hold;
            }
            // The bunt (§7.3): the batter at first by default; the lead force only when the bunt came too hard for the
            // sac (fielding.bunt.hardExitMph) and it is makeable; home on a squeeze only from inside bunt.squeezeHomeFt.
            // A popped bunt is a pop (§5.8): the catch and the doubled-off race above are its rows.
            if (v.Ball is { Shape: BattedBallClass.Bunt })
            {
                var b = R.Fielding.Bunt;
                if (DistTo(4) <= b.SqueezeHomeFt && (Makeable(4) || PlateWorthIt())) return PlayAt(v, 4);
                if (v.Hit is not null && BuntDefense.TooHard(v.Hit, b) && LeadForce() is > 0 and var hardLead && Makeable(hardLead))
                    return PlayAt(v, hardLead);
                if (v.Forces.At(1) && Makeable(1)) return PlayAt(v, 1);
                return CpuFieldDecision.Hold;
            }
            // 1. The lead forced bag ahead of a forced runner (second, third, home); the batter at first is rule 4.
            if (LeadForce() is > 0 and var lead && Makeable(lead)) return PlayAt(v, lead);
            // 2. Home, 3. third, then second (tags on a runner going): the lead body first, unless a trailing
            // body's margin is better by running.steal.cpuTrailPreferSec (§11.3, the double steal).
            var tagBag = 0;
            var tagMargin = double.NegativeInfinity;
            var prefer = R.Running.Steal.CpuTrailPreferSec;
            for (var bag = 4; bag >= 2; bag--)
            {
                if (v.Forces.At(bag)) continue;
                var m = Margin(bag);
                if (!(m > makeable || TagWorthIt(bag))) continue;
                if (tagBag == 0 || m >= tagMargin + prefer)
                {
                    tagBag = bag;
                    tagMargin = m;
                }
            }
            if (tagBag > 0) return PlayAt(v, tagBag);
            // 4. First.
            if (v.Forces.At(1) && Makeable(1)) return PlayAt(v, 1);
            // 5. Hold: nobody is out on a throw; Time comes when the bodies settle (§10.6).
            return CpuFieldDecision.Hold;
        }

        // Outfielders: home if a run is at stake, third, second, else the cutoff.
        var runAtStake = v.Outs < 2 || Math.Abs(v.HomeScore - v.AwayScore) <= 2;
        if (runAtStake && (Makeable(4) || PlateWorthIt())) return new(CpuFieldAction.ThrowTo, 4);
        if (Makeable(3)) return new(CpuFieldAction.ThrowTo, 3);
        if (Makeable(2)) return new(CpuFieldAction.ThrowTo, 2);
        return new(CpuFieldAction.ThrowIn);
    }

    /// <summary>
    /// The play the table chose at <paramref name="bag"/>: step on it when inside fielding.throw.unassistedFt (§10.4, S-41);
    /// otherwise whoever gets the ball there first makes it — this body's legs, or a throw to a cover who must be at the bag to
    /// take it (§8.5, §10.3: the catcher walks to the plate on a steal of home rather than lobbing at a bag nobody covers).
    /// </summary>
    public static CpuFieldDecision PlayAt(ICpuFieldView v, int bag)
    {
        var at = DiamondGeometry.Of(v.Rules).Bag(bag);
        var forceThere = v.Forces.At(bag) || v.Runners.Any(r => r.Live && r.LeftEarly && r.FromBag == bag);
        // A hot ball (§13) burns the glove that carries it too long: a walk that outlasts the hold is thrown instead, when a cover
        // can take the throw. With nobody to throw to the glove walks anyway and takes the drop, knowingly.
        var throwable = !double.IsPositiveInfinity(v.ThrowReadySec(bag));
        var walkHolds = HoldsOnFoot(v, bag) || !throwable;
        if (forceThere && walkHolds && Diamond.Dist(v.GloveX, v.GloveZ, at.X, at.Z) <= v.Rules.Fielding.Throw.UnassistedFt)
            return new(CpuFieldAction.WalkTo, bag);
        if (walkHolds && v.WalkSec(bag) <= v.ThrowReadySec(bag))
            return new(CpuFieldAction.WalkTo, bag);
        return new(CpuFieldAction.ThrowTo, bag);
    }

    /// <summary>The glove can carry the ball to <paramref name="bag"/> before a hot ball would drop (§13); always true for an ordinary ball.</summary>
    static bool HoldsOnFoot(ICpuFieldView v, int bag) => v.WalkSec(bag) < v.HoldLeftSec;

    /// <summary>
    /// Seconds until the glove can have the ball at <paramref name="bag"/> by the quicker of its legs and a throw (§8.8). A hot
    /// ball's walk counts only when it ends before the drop (§13).
    /// </summary>
    public static double PlayArrivalSec(ICpuFieldView v, int bag) =>
        Math.Min(HoldsOnFoot(v, bag) ? v.WalkSec(bag) : double.PositiveInfinity, v.ThrowReadySec(bag));
}
