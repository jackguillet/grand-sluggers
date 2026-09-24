namespace GrandSluggers.Sim;

/// <summary>Where the ball is, for the runner's estimate of the next throw (spec §9.9).</summary>
public sealed record BallSituation(
    /// <summary>A glove holds the ball (not in flight).</summary>
    bool Held,
    /// <summary>The ball is in flight to <see cref="ThrowBag"/>; <see cref="ThrowArrivesAt"/> is play seconds.</summary>
    bool Throwing,
    int ThrowBag,
    double ThrowArrivesAt,
    /// <summary>The glove on the ball (holding it, or the one that will meet it), and where it meets it.</summary>
    double GloveX,
    double GloveZ,
    /// <summary>Play seconds when a free ball is first in a glove (now when held).</summary>
    double MeetAt,
    /// <summary>The ball is on the outfield grass (or lands there): the outfield-hit rules apply.</summary>
    bool OnGrass,
    /// <summary>The ball's landing / current spot, for "in front of the runner" and the infield-in read.</summary>
    double BallX,
    double BallZ,
    double CarryFt,
    /// <summary>The batted ball is a bunt (§7.3): the runner from third holds at contact unless the offense sent them.</summary>
    bool Bunt = false,
    /// <summary>
    /// The runner's read of a throw released at (x, z) to a bag, in seconds (§9.9, #722): the defense's own plan — the
    /// arm, the relay and the chemistry as far as the rung reads them. Null reads the neutral flat throw, which is what
    /// the shipped rungs read too.
    /// </summary>
    Func<double, double, int, double>? ThrowClock = null,
    /// <summary>The position of the glove on the ball (holding it, or the one that will meet it); null when unknown.</summary>
    string? Fielder = null);

/// <summary>Everything the CPU runner reads at a decision event (§9.9).</summary>
public sealed record RunnerAiContext(
    double Elapsed,
    int Outs,
    /// <summary>Trailing by this many runs in the last scheduled inning or later (negative when leading).</summary>
    int TrailingInLastInning,
    FlyState Fly,
    BallSituation Ball,
    double Dash01,
    /// <summary>This decision is the catch itself (§9.5, #732): the one event a tag-up race is judged at. Later events with the fly caught leave a held runner held.</summary>
    bool AtCatch = false);

/// <summary>
/// The CPU baserunner (spec §9.9): evaluated at contact, at every fielder touch, at every throw
/// release, at the catch or the drop, and when a runner reaches a bag — events, not per frame.
/// <c>margin(next) = throwArrival(next) − runnerArrival(next)</c> from the runner's actual body
/// (<see cref="RunnerSystem.ArrivalSec"/>) and the defense's live estimate. Thresholds are
/// <c>running.cpu</c>; difficulty adds <c>cpu.*.runnerMarginSec</c>. It never touches a runner the
/// human owns.
/// </summary>
public static class RunnerAi
{
    /// <summary>
    /// Seconds until a throw could arrive at <paramref name="bag"/>: the ball in the glove (or met)
    /// plus the defense's reaction plus the live throw clock. One estimate for every runner.
    /// </summary>
    public static double ThrowArrivalSec(BallSituation ball, int bag, double elapsed, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var reaction = r.Running.Cpu.ReactionSec;
        // The clock the runner reads (#722): the defense's plan when the ball carries one, else the neutral flat throw.
        double Clock(double x, double z) => ball.ThrowClock?.Invoke(x, z, bag) ?? InPlay.ThrowArrivalSec(x, z, bag, null, r);
        if (ball.Throwing)
        {
            var lands = Math.Max(0, ball.ThrowArrivesAt - elapsed);
            var landing = Diamond.Bag(ball.ThrowBag);
            return ball.ThrowBag == bag ? lands : lands + reaction + Clock(landing.X, landing.Z);
        }
        var meet = ball.Held ? 0 : Math.Max(0, ball.MeetAt - elapsed);
        return meet + reaction + Clock(ball.GloveX, ball.GloveZ);
    }

    /// <summary>Seconds of slack a runner has to reach <paramref name="bag"/> ahead of the throw (positive is safe).</summary>
    public static double Margin(Runner runner, int bag, RunnerAiContext ctx, RulesTable? rules = null) =>
        ThrowArrivalSec(ctx.Ball, bag, ctx.Elapsed, rules) - RunnerSystem.ArrivalSec(runner, bag, ctx.Elapsed, ctx.Dash01, rules);

    /// <summary>
    /// The infield is back (§9.9): one of the four depth infielders meets the ball at or behind his own standard depth from
    /// home (<see cref="Diamond.Positions"/>). The depth comes from the diamond, so the read holds on any basepath. The
    /// pitcher and the catcher never field at a depth.
    /// </summary>
    static bool InfieldBack(BallSituation ball) =>
        ball.Fielder is { } pos && FieldingResolver.IsInfieldDepth(pos)
        && Diamond.Dist(ball.GloveX, ball.GloveZ, Diamond.Home.X, Diamond.Home.Z)
            >= Diamond.Dist(Diamond.Positions[pos].X, Diamond.Positions[pos].Z, Diamond.Home.X, Diamond.Home.Z);

    /// <summary>
    /// A grounder "in front" of a runner on second: to the left side, where the fielder looks at
    /// them (SS / 3B). A runner does not run at a ball in front of them (§9.9).
    /// </summary>
    public static bool InFront(Runner runner, double ballX) =>
        runner.Bag == 2 && ballX < -Rules.Default.Running.Bags.OccupyRadiusFt;

    /// <summary>Decide for every CPU-owned live runner. Forced runners and the fly hold are the runner tick's; this is the choice.</summary>
    public static void Decide(IReadOnlyList<Runner> runners, RunnerAiContext ctx, Func<int, bool> forceAt, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var cpu = r.Running.Cpu;
        var slack = r.Cpu.Active.RunnerMarginSec
                    - (ctx.TrailingInLastInning >= cpu.DesperateTrailRuns ? cpu.DesperateSec : 0);
        var ordered = runners.Where(x => x.Live).OrderByDescending(x => x.Progress).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var runner = ordered[i];
            var blocked = i > 0 && ordered[i - 1].Live && ordered[i - 1].Bag == runner.NextBag && !ordered[i - 1].Advancing;
            DecideOne(runner, ctx, forceAt, cpu, slack, blocked, r);
        }
    }

    static void DecideOne(Runner runner, RunnerAiContext ctx, Func<int, bool> forceAt, CpuRunnerRules cpu, double slack, bool blocked, RulesTable r)
    {
        var ball = ctx.Ball;
        var next = runner.NextBag;
        var forcedNow = runner.Forced && !runner.IsBatter && forceAt(next) && ctx.Fly is (FlyState.None or FlyState.Dropped);

        switch (ctx.Fly)
        {
            case FlyState.InAir:
                // Hold; the batter runs to first and stays (§9.5). The tick enforces it.
                return;
            case FlyState.Caught:
                if (runner.IsBatter || !runner.OnBag || runner.LeftEarly || ctx.Outs >= 3) return;
                // Tag-up table (§9.5): third goes on a deep fly with fewer than two outs; second goes for third on a deep fly to right.
                if (runner.Bag == 3 && ctx.Outs < 2 && ball.CarryFt >= cpu.TagThirdMinCarryFt)
                    runner.Send(4);
                else if (runner.Bag == 2 && ball.BallX > 0 && ball.OnGrass && ball.CarryFt >= cpu.TagSecondMinCarryFt && !blocked)
                    runner.Send(3);
                // The race (#732, decision 5 of #730): once, at the catch, a runner on second or third goes when the margin to the
                // next bag clears the bag's threshold plus the rung's slack — the same estimate every other CPU runner read uses,
                // the thrower's arm and the fielder's relay included (§9.9). The gates above are never as shipped; a table whose
                // thresholds no margin reaches decides by the gates alone.
                else if (ctx.AtCatch && !blocked && runner.Bag is 2 or 3 && (runner.Bag != 3 || ctx.Outs < 2)
                         && Margin(runner, next, ctx, r) > TagUpThresholdSec(runner.Bag, cpu) + slack)
                    runner.Send(next);
                return;
        }

        if (forcedNow)
        {
            if (runner.DestBag <= runner.Bag) runner.Send(next);
            return;
        }
        if (runner.IsBatter && runner.Bag == 0) return; // first is the batter's bag whatever happens

        // In a rundown the CPU runner runs away from the ball (§9.7): a throw to the bag ahead turns them back,
        // a throw to the bag behind sends them on — when the other bag is reachable ahead of the ball's next leg
        // (the §9.9 margin from where the throw lands). A body the ball beats both ways keeps going and takes the
        // tag at the bag rather than running into the glove that has it.
        if (runner.InRundown && ball.Throwing && ball.ThrowBag is >= 1 and <= 4)
        {
            if (runner.DestBag > runner.Bag && ball.ThrowBag == runner.DestBag)
            {
                var backSec = runner.Feet / RunnerSystem.SpeedFtPerSec(runner.Who, ctx.Dash01, r);
                if (ThrowArrivalSec(ball, runner.Bag, ctx.Elapsed, r) - backSec > slack) runner.Return();
            }
            else if (runner.DestBag <= runner.Bag && ball.ThrowBag == runner.Bag && next > runner.Bag
                     && Margin(runner, next, ctx, r) > slack)
            {
                runner.Send(next);
            }
            return;
        }

        // A body on its steal segment is committed (§11.2): the catcher's read never turns a stealer back; only the rundown above does.
        if (runner.Stealing) return;
        // Already running: turn back only early in the segment when the margin has gone (the reference's "keep going past 40%").
        if (runner.Advancing && runner.Feet > 0)
        {
            var fraction = runner.Feet / runner.SegmentFt;
            if (fraction < cpu.CommitFraction && Margin(runner, runner.DestBag, ctx, r) < Threshold(runner, ctx, cpu, slack, ball))
                runner.Return();
            return;
        }
        // On the bag, or the batter through first and coming straight back (§9.4): the round-first read.
        if (!(runner.OnBag || runner.OverrunProtected) || next > 4 || blocked) return;

        var margin = Margin(runner, next, ctx, r);
        if (runner.IsBatter)
        {
            // Rounding first (§9.9): reads the pickup.
            if (margin > cpu.RoundFirstSec - runner.Who.Stats.Run * cpu.RoundFirstPerRunSec + slack)
                runner.Send(next);
            return;
        }
        if (!ball.OnGrass)
        {
            // A grounder in the infield.
            if (runner.Bag == 3)
            {
                // A bunt is not the squeeze (§7.3): the runner from third holds until a glove has it; the send is the human's stick.
                if (ball.Bunt && !ball.Held && !ball.Throwing) return;
                // The infield-back read is the contact read (where the fielder will field it); once the ball is in a glove the margin decides.
                var infieldBack = !ball.Held && !ball.Throwing && InfieldBack(ball);
                var go = ctx.Outs == 2 || infieldBack || margin > cpu.ThirdHomeMarginSec + slack;
                if (go) runner.Send(4);
                return;
            }
            if (margin > cpu.GroundGoMarginSec + slack && !InFront(runner, ball.BallX))
                runner.Send(next);
            return;
        }
        if (margin > Threshold(runner, ctx, cpu, slack, ball))
            runner.Send(next);
    }

    /// <summary>
    /// The CPU steal table (§11.6), once per at-bat at SET: <c>P = base(Run) × situation</c>, base 0 at or
    /// below running.cpu.stealMinRun, linear through the Run 6 / 8 / 10 anchors; × two outs, × the captain
    /// slugger up, × 0 with a runner armed ahead (no double steal into a body), × 0 trailing by the table's
    /// runs. The lead runner is read first. Perfect steal by the difficulty's chance; otherwise the arm is
    /// SET's, exposed to the pickoff (D3). The rolls are the caller's seeded stream.
    /// </summary>
    public static IReadOnlyList<(Runner Runner, StealArm Arm)> StealPlan(
        IReadOnlyList<Runner> runners, bool captainUp, int outs, int trailingRuns, Random rng, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var cpu = r.Running.Cpu;
        var plan = new List<(Runner, StealArm)>();
        if (trailingRuns >= cpu.StealTrailingRuns) return plan;
        var armedAhead = false;
        foreach (var runner in runners.Where(x => x.Live && !x.IsBatter && x.OnBag).OrderByDescending(x => x.Bag))
        {
            var next = Baserunning.StealTarget(runner.Bag);
            if (next == 0) continue;
            var open = next == 4 || !runners.Any(o => o.Live && o != runner && o.Bag == next);
            var chance = open && !armedAhead ? StealBase(runner.Who.Stats.Run, cpu) : 0;
            if (outs == 2) chance *= cpu.StealTwoOutsMul;
            if (captainUp) chance *= cpu.StealCaptainUpMul;
            // One roll per runner whatever the chance, so the stream is the same shape for every table (S-92).
            var roll = rng.NextDouble();
            if (chance <= 0 || roll >= chance)
            {
                armedAhead = false;
                continue;
            }
            var perfect = rng.NextDouble() < r.Cpu.Active.PerfectStealChance;
            plan.Add((runner, perfect ? StealArm.Perfect : StealArm.Set));
            armedAhead = true;
        }
        return plan;
    }

    /// <summary>The base steal chance by Run (§11.6): 0 at or below the floor, then linear through the anchors.</summary>
    public static double StealBase(int run, CpuRunnerRules cpu)
    {
        if (run <= cpu.StealMinRun) return 0;
        static double Lerp(double a, double b, double t) => a + (b - a) * Math.Clamp(t, 0, 1);
        if (run <= 6) return Lerp(0, cpu.StealBaseRun6, (run - cpu.StealMinRun) / Math.Max(1.0, 6 - cpu.StealMinRun));
        if (run <= 8) return Lerp(cpu.StealBaseRun6, cpu.StealBaseRun8, (run - 6) / 2.0);
        return Lerp(cpu.StealBaseRun8, cpu.StealBaseRun10, (run - 8) / 2.0);
    }

    /// <summary>The tag-up threshold for a runner on <paramref name="bag"/> (§9.5, #732): home from third, third from second; nowhere else.</summary>
    public static double TagUpThresholdSec(int bag, CpuRunnerRules cpu) => bag switch
    {
        3 => cpu.TagUpHomeMarginSec,
        2 => cpu.TagUpThirdMarginSec,
        _ => double.PositiveInfinity
    };

    /// <summary>The outfield "go" threshold for this runner (§9.9): aggression by Run, more with two outs, more when desperate.</summary>
    static double Threshold(Runner runner, RunnerAiContext ctx, CpuRunnerRules cpu, double slack, BallSituation ball)
    {
        if (runner.IsBatter && runner.Bag == 1)
            return cpu.RoundFirstSec - runner.Who.Stats.Run * cpu.RoundFirstPerRunSec + slack;
        var t = ball.OnGrass
            ? cpu.OutfieldGoSec - runner.Who.Stats.Run * cpu.OutfieldGoPerRunSec - (ctx.Outs == 2 ? cpu.TwoOutsGoBonusSec : 0)
            : runner.Bag == 3 ? cpu.ThirdHomeMarginSec : cpu.GroundGoMarginSec;
        return t + slack;
    }
}
