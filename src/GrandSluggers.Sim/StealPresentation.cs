namespace GrandSluggers.Sim.Front;

/// <summary>Presentation input edges and authored take clocks; baseball remains in PitchSetupSystem.</summary>
public static class StealPresentation
{
    /// <summary>A target plus a fresh press is a base throw; choosing a target while holding a committed pitch is a step-off attempt.</summary>
    public static bool BaseThrow(int bag, int previousBag, bool pressed, bool held, bool committed) =>
        bag is >= 1 and <= 4 && (pressed || committed && held && bag != previousBag);

    /// <summary>
    /// Warp a throw take's release marker (<paramref name="releaseAt"/>, the played take's <c>Release</c>) onto the sim's
    /// preparation clock, then play its follow-through at the take's own speed. The ball leaves the hand when the sim says.
    /// </summary>
    public static double ThrowSample(double commandTime, double preparationSeconds, double releaseAt) =>
        commandTime < preparationSeconds && preparationSeconds > 0
            ? Math.Max(0, commandTime) / preparationSeconds * releaseAt
            : releaseAt + Math.Max(0, commandTime - preparationSeconds);

    /// <summary>
    /// The thrower's take (#966): on the runner play the catcher receives in the crouch, transfers, plants and throws
    /// (<see cref="Motion.Verb.CatcherThrow"/>); every other throw is the ordinary one.
    /// </summary>
    public static Motion.Verb ThrowVerb(string fromPos, bool runnerPlay) =>
        runnerPlay && fromPos == "C" ? Motion.Verb.CatcherThrow : Motion.Verb.Throw;

    /// <summary>The bag a live runner is bound for: the one it returns to, the one it advances or slides to, else none (0).</summary>
    public static int BoundFor(Runner runner) =>
        !runner.Live ? 0
        : runner.Phase == RunnerPhase.Returning ? runner.Bag
        : runner.DestBag > runner.Bag ? runner.DestBag
        : 0;

    /// <summary>
    /// The sweep tag (#966): the ball-holder at <paramref name="gloveX"/>, <paramref name="gloveZ"/> stands within
    /// <paramref name="standFt"/> of a bag, and a live runner bound for that bag and not yet on it is within
    /// <paramref name="windowFt"/> of it. Presentation only: whether the tag counts is the sim's reach (§10.3).
    /// </summary>
    public static bool Tagging(double gloveX, double gloveZ, IEnumerable<Runner> runners, DiamondGeometry diamond,
        double standFt, double windowFt)
    {
        foreach (var runner in runners)
        {
            var bag = BoundFor(runner);
            if (bag == 0 || runner.IsOn(bag)) continue;
            var at = diamond.Bag(bag);
            if (Diamond.Dist(gloveX, gloveZ, at.X, at.Z) > standFt) continue;
            var (x, z) = runner.Position;
            if (Diamond.Dist(x, z, at.X, at.Z) <= windowFt) return true;
        }
        return false;
    }

    /// <summary>A runner reversed (#966): it was advancing or stealing and now returns to the bag it left.</summary>
    public static bool TurnedBack(RunnerPhase before, RunnerPhase now) =>
        now == RunnerPhase.Returning && before is RunnerPhase.Advancing or RunnerPhase.Stealing;

    /// <summary>
    /// A live runner's take (#966): the slide; the plant-and-turn for <see cref="Motion.TurnBackDur"/> after it reversed; the
    /// run while it moves free; else standing. The head-first slide is a slot nobody plays until its runners are chosen.
    /// </summary>
    public static Motion.Verb RunnerVerb(Runner runner, double sinceTurnBackSec) =>
        runner.Sliding ? Motion.Verb.Slide
        : runner.Phase == RunnerPhase.Returning && sinceTurnBackSec >= 0 && sinceTurnBackSec < Motion.TurnBackDur ? Motion.Verb.TurnBack
        : runner.Moving && !runner.Held ? Motion.Verb.Run
        : Motion.Verb.Idle;
}
