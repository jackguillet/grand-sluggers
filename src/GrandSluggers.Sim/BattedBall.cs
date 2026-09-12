namespace GrandSluggers.Sim;

/// <summary>The one batted-ball class table (spec §6.2). Fielding assignment, cameras, and the runner AI read it.</summary>
public enum BattedBallClass
{
    /// <summary>Weak roller in front of the plate.</summary>
    Topper,
    /// <summary>Infield hop.</summary>
    Grounder,
    /// <summary>High first bounce, slow to the glove.</summary>
    Chopper,
    /// <summary>Rope; catchable inside the short window.</summary>
    Liner,
    /// <summary>Infield fly: long hang, lands inside the lip.</summary>
    Pop,
    /// <summary>Outfield fly: routine or deep by carry.</summary>
    Fly,
    /// <summary>A fly or liner that meets the fence below fence height: the carom.</summary>
    Wall,
    /// <summary>Crossed the fence above height between the poles: dead, runners circle.</summary>
    Homer,
    /// <summary>The bunt verb: a dribbler in the triangle.</summary>
    Bunt,
    /// <summary>Lands or is touched in foul territory: dead unless caught.</summary>
    Foul
}

public static class BattedBallClasses
{
    /// <summary>The gloves scoop it on the dirt (no ring, no window).</summary>
    public static bool OnTheDirt(this BattedBallClass c) =>
        c is BattedBallClass.Topper or BattedBallClass.Grounder or BattedBallClass.Chopper or BattedBallClass.Bunt;

    /// <summary>It is up: a landing ring and a catch window.</summary>
    public static bool InTheAir(this BattedBallClass c) =>
        c is BattedBallClass.Liner or BattedBallClass.Pop or BattedBallClass.Fly or BattedBallClass.Wall or BattedBallClass.Homer;

    /// <summary>The camera pulls back for these (a fly, not a rope on the dirt).</summary>
    public static bool IsFlyShape(this BattedBallClass c) =>
        c is BattedBallClass.Pop or BattedBallClass.Fly or BattedBallClass.Wall or BattedBallClass.Homer;

    /// <summary>
    /// The coarse read at the crack from launch and exit alone (flight.classes): topper, grounder,
    /// liner, or fly. The flight refines it (chopper, pop, wall, homer) in <see cref="BattedBall.Of(double, double, double, bool, Park, RulesTable?)"/>.
    /// </summary>
    public static BattedBallClass ByLaunch(double launchDeg, double exitMph, RulesTable? rules = null)
    {
        var c = Rules.Or(rules).Flight.Classes;
        if (launchDeg < c.TopperMaxLaunchDeg) return BattedBallClass.Topper;
        if (launchDeg < c.GrounderMaxLaunchDeg) return BattedBallClass.Grounder;
        if (launchDeg < c.ChopperMaxLaunchDeg)
            return exitMph >= c.LinerMinExitMph ? BattedBallClass.Liner : BattedBallClass.Grounder;
        if (launchDeg < c.LinerMaxLaunchDeg && exitMph >= c.LinerMinExitMph) return BattedBallClass.Liner;
        return BattedBallClass.Fly;
    }
}

/// <summary>
/// One batted ball in one park: the clipped path (<see cref="BallFlight"/>) and every fact the
/// play reads from it — its class, where it lands, whether it met the wall or cleared the fence,
/// and the fair / foul verdict of the untouched path (spec §5.6, §6.1, §6.2). Computed once from
/// contact (exit, launch, spray) and the park; the same rule serves the flight, the fielding
/// preview, and the landing ring.
/// </summary>
public sealed record BattedBall(
    IReadOnlyList<Sample> Samples,
    BattedBallClass Shape,
    bool Foul,
    double DecidedT,
    double DecidedX,
    double DecidedZ,
    bool HomeRun,
    bool GroundRule,
    double? LeavesT,
    double HangT,
    double LandingX,
    double LandingZ,
    double LandingDist,
    double? WallT,
    double FenceClearFt)
{
    /// <summary>The ball leaves the field over a wall in the air: catching it there is a rob at that wall's top (§8.4).</summary>
    public bool LeavesInTheAir => LeavesT is { } t && WallT is null && !GroundRule && t <= HangT + 1e-9;

    /// <summary>The spec's class: <see cref="BattedBallClass.Foul"/> when the untouched path is foul, else the shape.</summary>
    public BattedBallClass Class => Foul ? BattedBallClass.Foul : Shape;

    /// <summary>The ball met the outfield fence below its top before touching the ground.</summary>
    public bool MetTheWall => Shape == BattedBallClass.Wall;

    /// <summary>Height above the top of the wall the ball left over (the fence or a foul wall); NaN if it stayed in.</summary>
    public double WallClearFt => FenceClearFt;

    /// <summary>The ball leaves the park (over the fence or into the foul stands).</summary>
    public bool Leaves => LeavesT is not null;

    public static BattedBall Of(AtBatResult hit, Park park, RulesTable? rules = null) =>
        Of(hit.ExitVeloMph, hit.LaunchDeg, hit.SprayDeg, hit.Class == BattedBallClass.Bunt, park, rules);

    public static BattedBall Of(double exitMph, double launchDeg, double sprayDeg, Park park, RulesTable? rules = null) =>
        Of(exitMph, launchDeg, sprayDeg, false, park, rules);

    public static BattedBall Of(double exitMph, double launchDeg, double sprayDeg, bool bunt, Park park, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var samples = BallFlight.Trajectory(exitMph, launchDeg, sprayDeg, park, r);
        return Read(samples, exitMph, launchDeg, bunt, park, r);
    }

    static BattedBall Read(IReadOnlyList<Sample> samples, double exitMph, double launchDeg, bool bunt, Park park, RulesTable rules)
    {
        var c = rules.Flight.Classes;
        var grounded = false;
        var decided = false;
        var foul = false;
        var decidedT = 0.0;
        var decidedX = 0.0;
        var decidedZ = 0.0;
        var homer = false;
        var groundRule = false;
        double? leavesT = null;
        double? wallT = null;
        var clear = double.NaN;
        var firstGround = -1;
        var secondGround = -1;

        void Decide(bool isFoul, Sample at)
        {
            if (decided) return;
            decided = true;
            foul = isFoul;
            decidedT = at.T;
            decidedX = at.X;
            decidedZ = at.Z;
        }

        for (var i = 1; i < samples.Count; i++)
        {
            var s = samples[i];
            var prev = samples[i - 1];
            switch (s.Event)
            {
                case SampleEvent.Fence:
                    if (leavesT is null)
                    {
                        leavesT = s.T;
                        if (grounded) groundRule = true;
                        else homer = true;
                        clear = s.Height - park.FenceHeightFt;
                        Decide(false, s);
                    }
                    break;
                case SampleEvent.Stands:
                    if (leavesT is null)
                    {
                        leavesT = s.T;
                        clear = s.Height - FieldBounds.FoulWallHeightFt;
                        // A ball already fair that bounces into the foul stands is out of play the same way
                        // as one that hops the fence: two bases (§1). Anything else over a foul wall is foul.
                        if (decided && !foul) groundRule = true;
                        else Decide(true, s);
                    }
                    break;
                case SampleEvent.FoulWall:
                    Decide(true, s);
                    break;
                case SampleEvent.Wall:
                    if (wallT is null && !grounded)
                    {
                        wallT = s.T;
                        clear = s.Height - park.FenceHeightFt;
                    }
                    Decide(false, s);
                    break;
                case SampleEvent.Ground:
                    if (!grounded)
                    {
                        grounded = true;
                        firstGround = i;
                        if (FieldBounds.PastTheBags(s.X, s.Z))
                            Decide(!FieldBounds.IsFair(s.X, s.Z), s);
                    }
                    else if (secondGround < 0 && prev.Event != SampleEvent.Ground)
                        secondGround = i;
                    break;
            }
            if (grounded && !decided && !FieldBounds.PastTheBags(prev.X, prev.Z) && FieldBounds.PastTheBags(s.X, s.Z))
                Decide(!FieldBounds.IsFair(s.X, s.Z), s);
        }
        if (!decided && samples.Count > 0)
        {
            var last = samples[^1];
            Decide(!FieldBounds.IsFair(last.X, last.Z), last);
        }

        var landing = BallFlight.LandingIndex(samples);
        var mark = landing < 0 ? samples[^1] : samples[landing];
        var landingDist = mark.Dist;

        BattedBallClass shape;
        // A bunt popped up (§5.8) is a pop the corners and the catcher can take (§7.3), not a dribbler.
        if (bunt && launchDeg <= c.LinerMaxLaunchDeg) shape = BattedBallClass.Bunt;
        else if (homer) shape = BattedBallClass.Homer;
        else if (wallT is not null) shape = BattedBallClass.Wall;
        else
        {
            shape = BattedBallClasses.ByLaunch(launchDeg, exitMph, rules);
            // A chopper is a hard ball driven into the dirt in front of the plate: any launch under the
            // chopper band (the topper band included) whose first hop is high and close.
            if (launchDeg < c.ChopperMaxLaunchDeg
                && exitMph >= c.ChopperMinExitMph && firstGround > 0
                && samples[firstGround].Dist < c.ChopperFirstBounceFt
                && BouncePeak(samples, firstGround, secondGround) > c.ChopperBounceHeightFt)
                shape = BattedBallClass.Chopper;
            else if (shape == BattedBallClass.Fly && landingDist < c.InfieldLipFt)
                shape = BattedBallClass.Pop;
        }

        return new BattedBall(
            samples, shape, foul, decidedT, decidedX, decidedZ, homer, groundRule, leavesT,
            mark.T, mark.X, mark.Z, landingDist, wallT, clear);
    }

    static double BouncePeak(IReadOnlyList<Sample> samples, int firstGround, int secondGround)
    {
        var end = secondGround > firstGround ? secondGround : samples.Count;
        var peak = 0.0;
        for (var i = firstGround + 1; i < end; i++)
            peak = Math.Max(peak, samples[i].Height);
        return peak;
    }
}
