namespace GrandSluggers.Sim;

/// <summary>What a runner is doing (spec §9.1). Out and Scored are final for the play.</summary>
public enum RunnerPhase
{
    OnBag,
    Advancing,
    Returning,
    Stealing,
    Sliding,
    Out,
    Scored
}

/// <summary>A catchable ball in the air holds every runner (spec §9.5); the catch or the drop frees them.</summary>
public enum FlyState
{
    /// <summary>A grounder, a hop, or a ball already on the ground: runners run.</summary>
    None,
    /// <summary>In the air, uncaught: every runner returns to the bag and cannot leave.</summary>
    InAir,
    /// <summary>Firmly caught: a runner on the bag may tag up; one off the bag must retouch.</summary>
    Caught,
    /// <summary>Landed uncaught: live, forced runners are forced.</summary>
    Dropped
}

/// <summary>
/// A body on the basepath (spec §9.1): a bag index plus feet along the 90 ft segment toward the
/// next bag, a velocity, a phase, a destination, and the forced flag snapshotted at contact.
/// The batter-runner is one too (<see cref="FromBag"/> 0), starting from the box. Every tag,
/// force, and arrival reads <see cref="Position"/>; nothing re-derives a runner from a table.
/// </summary>
public sealed class Runner
{
    public Character Who { get; }
    /// <summary>The bag this runner started the play on; 0 is the batter-runner. Outs and moves are recorded by it.</summary>
    public int FromBag { get; private set; }
    /// <summary>The last bag touched. 0 is the plate for the batter-runner on the way to first; 4 is home.</summary>
    public int Bag { get; private set; }
    /// <summary>Feet along the segment from <see cref="Bag"/> toward the next bag.</summary>
    public double Feet { get; private set; }
    public RunnerPhase Phase { get; private set; } = RunnerPhase.OnBag;
    /// <summary>The bag this runner is heading for; equal to <see cref="Bag"/> when holding.</summary>
    public int DestBag { get; private set; }
    /// <summary>Forced off the bag at contact (a runner behind on every bag back to the batter).</summary>
    public bool Forced { get; private set; }
    /// <summary>Feet per second along the path this frame; negative on a return.</summary>
    public double Velocity { get; private set; }
    /// <summary>A halt from the offense pad: the body stops where it is until sent or returned.</summary>
    public bool Held { get; private set; }
    /// <summary>The steal arm and when it was made (spec §11.1, §11.2): SET, the windup, or the perfect window.</summary>
    public StealArm StealArm { get; private set; }
    /// <summary>A steal is armed on this runner.</summary>
    public bool StealArmed => StealArm != StealArm.None;
    /// <summary>The armed runner has left the bag on the pitch or the pickoff motion: the steal is a body in motion (§11.2).</summary>
    public bool Broke { get; private set; }
    /// <summary>Seconds settled on <see cref="Bag"/>; 0 while moving (Time reads it, §10.6).</summary>
    public double OnBagSec { get; private set; }
    /// <summary>Play seconds when home was crossed; NaN otherwise (the third-out rule, §1).</summary>
    public double ScoredAt { get; private set; } = double.NaN;
    /// <summary>Play seconds of the last bag touched; NaN before the first.</summary>
    public double LastTouchAt { get; private set; } = double.NaN;
    /// <summary>All-advance pressed before the catch: wait on the bag, leave at the catch (§9.5).</summary>
    public bool TagAndGo { get; private set; }
    /// <summary>The offense sent this runner by hand; the fly hold does not pull them back.</summary>
    public bool HumanSent { get; private set; }
    /// <summary>Off the start bag when the fly was caught: must retouch it before advancing (§10.5).</summary>
    public bool LeftEarly { get; private set; }
    /// <summary>West / South near the bag forces the slide (§9.4).</summary>
    public bool ForceSlide { get; private set; }
    /// <summary>Where the batter-runner's first segment begins: the box at contact. Bags for everyone else.</summary>
    public (double X, double Z) Start { get; private set; }
    /// <summary>Set by the runner tick on the frame a bag is touched; the runner AI re-reads the play on it.</summary>
    public bool ArrivedThisTick { get; internal set; }
    /// <summary>Feet past first along the line from home, running through the bag (§9.4). 0 everywhere else.</summary>
    public double OverrunFt { get; private set; }
    /// <summary>Still running out through first; false once turned back toward the bag.</summary>
    public bool OverrunOut { get; private set; }
    /// <summary>Caught between bags with a glove holding the ball in range (§9.7). Set by the live ball each frame.</summary>
    public bool InRundown { get; private set; }

    /// <summary>The live ball's read of the rundown this frame (§9.7).</summary>
    public void MarkRundown(bool on) => InRundown = on;

    public Runner(Character who, int bag)
    {
        Who = who;
        Seat(bag);
    }

    /// <summary>The batter-runner at contact: from the box, bound for first.</summary>
    public static Runner BatterRunner(Character who, double startX, double startZ)
    {
        var r = new Runner(who, 0) { Start = (startX, startZ), DestBag = 1 };
        return r;
    }

    public bool Live => Phase is not (RunnerPhase.Out or RunnerPhase.Scored);
    public bool IsBatter => FromBag == 0;
    /// <summary>Settled on the bag: standing there with nowhere else to be. A body just sent, or owing a retouch, is not (Time reads this, §10.6).</summary>
    public bool OnBag => Phase == RunnerPhase.OnBag && DestBag == Bag && !LeftEarly;
    public bool Moving => Phase is RunnerPhase.Advancing or RunnerPhase.Sliding or RunnerPhase.Returning or RunnerPhase.Stealing;
    public bool Sliding => Phase == RunnerPhase.Sliding;
    /// <summary>Past first on the run-through (§9.4).</summary>
    public bool Overrunning => OverrunFt > 0;
    /// <summary>Running through first and coming straight back: the bag is theirs, no tag reaches them (§9.4, §10.3).</summary>
    public bool OverrunProtected => Overrunning && DestBag <= 1;
    public bool Scored => Phase == RunnerPhase.Scored;
    public bool Out => Phase == RunnerPhase.Out;
    /// <summary>Heading for a bag beyond the last one touched.</summary>
    public bool Advancing => Live && DestBag > Bag && !Held;
    public int StealTarget => StealArmed ? Baserunning.StealTarget(FromBag) : 0;
    /// <summary>On the steal segment: armed, broke, and not past the bag they broke toward (the runner AI never second-guesses this body, §9.9).</summary>
    public bool Stealing => StealArmed && Broke && Bag == FromBag;

    /// <summary>The next bag on the path, 4 at home.</summary>
    public int NextBag => Math.Min(4, Bag + 1);

    /// <summary>Length of the segment the runner is on: box to first for the batter, 90 ft otherwise.</summary>
    public double SegmentFt => Bag == 0 ? Math.Max(1, Diamond.Dist(Start.X, Start.Z, Diamond.First.X, Diamond.First.Z)) : Diamond.Baseline;

    /// <summary>Feet along the whole path, home to home, for ordering and the no-pass rule.</summary>
    public double Progress => Bag >= 4 ? 4 * Diamond.Baseline : Bag * Diamond.Baseline + Feet * (Diamond.Baseline / SegmentFt);

    public (double X, double Z) Position
    {
        get
        {
            if (Bag >= 4) return Diamond.Home;
            if (Overrunning && Bag == 1 && Feet <= 0)
            {
                // Through the bag on the line from home, past first (§9.4).
                var first = Diamond.First;
                var len = Math.Max(1, Diamond.Dist(Diamond.Home.X, Diamond.Home.Z, first.X, first.Z));
                var ux = (first.X - Diamond.Home.X) / len;
                var uz = (first.Z - Diamond.Home.Z) / len;
                return (first.X + ux * OverrunFt, first.Z + uz * OverrunFt);
            }
            var from = Bag == 0 ? Start : Diamond.Bag(Bag);
            var to = Diamond.Bag(Bag + 1);
            var u = Math.Clamp(Feet / SegmentFt, 0, 1);
            return (from.X + (to.X - from.X) * u, from.Z + (to.Z - from.Z) * u);
        }
    }

    /// <summary>Feet still to run to reach <paramref name="bag"/> along the path; 0 when already there or past it.</summary>
    public double FeetTo(int bag)
    {
        if (bag <= Bag) return 0;
        var first = SegmentFt - Feet;
        // Past first on the run-through: the way back to the bag comes first (§9.4).
        return first + (bag - Bag - 1) * Diamond.Baseline + OverrunFt;
    }

    /// <summary>Feet back along the path to <see cref="FromBag"/> (the doubled-off race, §10.5): 0 when standing there.</summary>
    public double FeetBackToStart()
    {
        if (!Live || IsBatter) return 0;
        if (Bag < FromBag) return 0;
        return Feet + (Bag - FromBag) * Diamond.Baseline;
    }

    /// <summary>Standing on <paramref name="bag"/>: the last bag touched, feet 0. A body through first and coming straight back holds it (§9.4).</summary>
    public bool IsOn(int bag) => Live && Bag == bag && Feet <= 0 && (Phase != RunnerPhase.Returning || OverrunProtected);

    /// <summary>Stand this runner on a bag between plays (or at Complete): the bag is now where they start from.</summary>
    public void Seat(int bag)
    {
        FromBag = bag;
        Bag = bag;
        Feet = 0;
        Phase = RunnerPhase.OnBag;
        DestBag = bag;
        Forced = false;
        Velocity = 0;
        Held = false;
        StealArm = StealArm.None;
        Broke = false;
        OnBagSec = 0;
        ScoredAt = double.NaN;
        LastTouchAt = double.NaN;
        TagAndGo = false;
        HumanSent = false;
        LeftEarly = false;
        ForceSlide = false;
        ArrivedThisTick = false;
        OverrunFt = 0;
        OverrunOut = false;
        InRundown = false;
    }

    /// <summary>Contact: snapshot the force, clear the play flags. The steal arm survives (P6 reads it).</summary>
    public void BeginPlay(bool forced, bool tagAndGo)
    {
        Forced = forced;
        Phase = RunnerPhase.OnBag;
        DestBag = IsBatter ? 1 : Bag;
        Velocity = 0;
        Held = false;
        OnBagSec = 0;
        ScoredAt = double.NaN;
        LastTouchAt = double.NaN;
        TagAndGo = tagAndGo;
        HumanSent = false;
        LeftEarly = false;
        ForceSlide = false;
        ArrivedThisTick = false;
        OverrunFt = 0;
        OverrunOut = false;
        InRundown = false;
    }

    /// <summary>Head for <paramref name="bag"/> (never back past the last touched bag; home at most).</summary>
    public void Send(int bag, bool human = false)
    {
        if (!Live) return;
        DestBag = Math.Clamp(bag, Bag, 4);
        Held = false;
        if (human) HumanSent = true;
        if (LeftEarly) DestBag = Bag; // must retouch first (§10.5); the return decides the rest
        if (Overrunning && DestBag > 1) OverrunOut = false; // turned toward second: back to the bag first, live (§9.4)
    }

    /// <summary>Come back to the last bag touched (to the start bag after a catch, §9.5).</summary>
    public void Return(bool human = false)
    {
        if (!Live) return;
        DestBag = LeftEarly ? FromBag : Bag;
        Held = false;
        if (human) HumanSent = false;
        TagAndGo = false;
    }

    /// <summary>Freeze where they stand (§9.3). A send or a return clears it.</summary>
    public void Halt()
    {
        if (!Live) return;
        // Fair contact always sends the batter (§7): there is no halting them in the box.
        if (IsBatter && Bag == 0) return;
        Held = true;
        Velocity = 0;
        TagAndGo = false;
        // Halted on the bag: the bag is where they stay (a send is cancelled; Time may read them as settled, §10.6).
        if (Feet <= 0 && !Overrunning && Bag >= 1) DestBag = Bag;
    }

    public void SetTagAndGo(bool on)
    {
        if (Live) TagAndGo = on;
    }

    /// <summary>Arm the steal (§11.1). Nothing else changes until the break.</summary>
    public void ArmSteal(StealArm arm = StealArm.Set)
    {
        if (Live && arm != StealArm.None) StealArm = arm;
    }

    /// <summary>Stick back before the pitch: the arm comes off. A body that already broke stays a body (the return is <see cref="Return"/>).</summary>
    public void CancelSteal()
    {
        if (Broke) return;
        StealArm = StealArm.None;
    }

    /// <summary>The pitch (or the pickoff motion) came: this armed runner has broken (§11.2). The body is placed when the live ball begins.</summary>
    public void MarkBroke()
    {
        if (Live && StealArmed && !IsBatter) Broke = true;
    }

    /// <summary>
    /// The break (§11.2, D2 / D3): the armed body leaves the bag toward its steal target with the
    /// head start the clock gave it (<see cref="StealBreak.HeadStartFt"/>), full speed from here.
    /// </summary>
    public void Break(double headStartFt)
    {
        if (!Live || !StealArmed || IsBatter) return;
        Broke = true;
        Held = false;
        DestBag = Math.Min(4, Bag + 1);
        Feet = Math.Clamp(headStartFt, 0, Diamond.Baseline);
        Phase = RunnerPhase.Stealing;
    }
    public void RequestSlide() => ForceSlide = true;

    /// <summary>The fly was caught with this runner off the start bag: they owe a retouch (§10.5).</summary>
    public void MarkLeftEarly()
    {
        if (!Live || IsBatter) return;
        LeftEarly = true;
        DestBag = FromBag;
        Held = false;
    }

    /// <summary>Snap onto a bag: a verdict awarded it (beat the throw), or a walk placed them there.</summary>
    public void Arrive(int bag, double at)
    {
        if (!Live) return;
        if (bag >= 4)
        {
            Score(at);
            return;
        }
        Bag = bag;
        Feet = 0;
        OverrunFt = 0;
        OverrunOut = false;
        Phase = RunnerPhase.OnBag;
        DestBag = bag;
        Velocity = 0;
        LastTouchAt = at;
        if (LeftEarly && bag == FromBag) LeftEarly = false;
    }

    public void Retire()
    {
        Phase = RunnerPhase.Out;
        Velocity = 0;
        DestBag = Bag;
    }

    public void Score(double at)
    {
        Bag = 4;
        Feet = 0;
        Phase = RunnerPhase.Scored;
        DestBag = 4;
        Velocity = 0;
        ScoredAt = at;
        LastTouchAt = at;
    }

    // ---- the tick (called by RunnerSystem only) ----

    internal void SetVelocity(double v) => Velocity = v;
    /// <summary>The batter-runner touched first stopping there: run through it (§9.4).</summary>
    internal void BeginOverrun(double startFt)
    {
        OverrunFt = Math.Max(1e-3, startFt);
        OverrunOut = true;
    }
    internal void SetOverrun(double feet, bool outward)
    {
        OverrunFt = Math.Max(0, feet);
        OverrunOut = outward && OverrunFt > 0;
    }
    internal void SetFeet(double feet) => Feet = feet;
    internal void SetPhase(RunnerPhase phase) => Phase = phase;
    internal void SetDest(int bag) => DestBag = Math.Clamp(bag, 0, 4);
    internal void TickOnBag(double dt) => OnBagSec = OnBag ? OnBagSec + dt : 0;
    internal void ClearForceSlide() => ForceSlide = false;

    internal void TouchBag(int bag, double at)
    {
        Bag = bag;
        LastTouchAt = at;
        if (LeftEarly && bag == FromBag) LeftEarly = false;
    }

    internal void RetreatToSegmentTop(int bag)
    {
        Bag = bag;
        Feet = Diamond.Baseline;
    }
}

/// <summary>Everything the runner tick reads from the live ball this frame.</summary>
public sealed record RunnerTickContext(
    double Elapsed,
    double Dash01,
    FlyState Fly,
    int Outs,
    /// <summary>The live force chain: a forced runner cannot be held while the force at their next bag stands (§9.3).</summary>
    Func<int, bool> ForceAt,
    /// <summary>A throw is armed to this bag, or a glove with the ball is close to it: the runner slides in (§9.4).</summary>
    Func<int, bool> TagThreatAt);

/// <summary>
/// Moves every body one frame (spec §9.1, §9.4, §9.5): one speed formula, the batter's start
/// delay, the no-pass rule, the fly hold, the slide, the home clamp on an unresolved fly.
/// </summary>
public static class RunnerSystem
{
    /// <summary>Seconds per 90 ft for this runner: <c>baseSec − Run × secPerRun</c>, clamped (§9.1).</summary>
    public static double BagSec(Character who, RulesTable? rules = null)
    {
        var s = Rules.Or(rules).Running.BagSec;
        return Math.Clamp(s.BaseSec - who.Stats.Run * s.SecPerRun, s.MinSec, s.MaxSec);
    }

    /// <summary>Feet per second on the path, with the dash (§9.4).</summary>
    public static double SpeedFtPerSec(Character who, double dash01 = 0, RulesTable? rules = null)
    {
        var s = Rules.Or(rules).Running.BagSec;
        return Diamond.Baseline / BagSec(who, rules) * (1 + s.DashMul * Math.Clamp(dash01, 0, 1));
    }

    /// <summary>
    /// The sim query P4's fielder reads (spec §8.8, §10.2): seconds until this runner would touch
    /// <paramref name="bag"/> running from where they are now at their speed (the batter's start
    /// delay included); 0 when already there or past it. Whether they intend to go is
    /// <see cref="Runner.DestBag"/>, not this.
    /// </summary>
    public static double ArrivalSec(Runner runner, int bag, double elapsed, double dash01 = 0, RulesTable? rules = null)
    {
        if (!runner.Live) return double.PositiveInfinity;
        var feet = runner.FeetTo(bag);
        if (feet <= 0) return 0;
        var wait = runner.IsBatter && runner.Bag == 0
            ? Math.Max(0, Rules.Or(rules).Running.BagSec.BatterStartSec - elapsed)
            : 0;
        return wait + feet / SpeedFtPerSec(runner.Who, dash01, rules);
    }

    /// <summary>
    /// Seconds until this runner is back on their start bag from where they are (§10.5): the
    /// doubled-off race the fielder reads. 0 when standing there; infinite when not live.
    /// </summary>
    public static double ReturnSec(Runner runner, double dash01 = 0, RulesTable? rules = null)
    {
        if (!runner.Live) return double.PositiveInfinity;
        var feet = runner.FeetBackToStart();
        return feet <= 0 ? 0 : feet / SpeedFtPerSec(runner.Who, dash01, rules);
    }

    /// <summary>The live runner heading for <paramref name="bag"/> (or standing short of it, bound there), nearest first.</summary>
    public static Runner? HeadingTo(IEnumerable<Runner> runners, int bag) =>
        runners.Where(r => r.Live && r.Bag < bag && r.DestBag >= bag)
            .OrderByDescending(r => r.Progress)
            .FirstOrDefault();

    public static void Tick(IReadOnlyList<Runner> runners, double dt, RunnerTickContext ctx, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var bagRules = r.Running.Bags;
        var speedRules = r.Running.BagSec;
        // Leaders move first so a trailer can be held behind them (no passing, §9.1).
        var ordered = runners.Where(x => x.Live).OrderByDescending(x => x.Progress).ToList();
        Runner? ahead = null;
        foreach (var runner in ordered)
        {
            runner.ArrivedThisTick = false;
            var forcedNow = runner.Forced && !runner.IsBatter && ctx.Fly is (FlyState.None or FlyState.Dropped) && ctx.ForceAt(runner.NextBag);

            // The fly hold (§9.5): in the air, everyone but the batter comes back to the bag and waits,
            // unless the offense sent them by hand. The batter holds at first once there.
            if (ctx.Fly == FlyState.InAir && !runner.HumanSent)
            {
                if (!runner.IsBatter) runner.SetDest(runner.Bag);
                else if (runner.Bag >= 1) runner.SetDest(Math.Min(runner.DestBag, runner.Bag));
            }
            // Forced on a grounder: they have no choice (§9.3).
            if (forcedNow && runner.DestBag <= runner.Bag && !runner.LeftEarly)
                runner.Send(runner.NextBag);
            // The batter always runs on fair contact.
            if (runner.IsBatter && runner.Bag == 0 && runner.DestBag < 1) runner.SetDest(1);

            var held = runner.Held && !forcedNow;
            var speed = SpeedFtPerSec(runner.Who, ctx.Dash01, r);
            var waiting = runner.IsBatter && runner.Bag == 0 && ctx.Elapsed < speedRules.BatterStartSec;

            // Through first (§9.4): out to overrunFt, then straight back; sent on, they turn back at once and are live.
            if (runner.Overrunning)
            {
                if (runner.OverrunOut && runner.DestBag <= 1)
                {
                    var outFt = runner.OverrunFt + speed * dt;
                    runner.SetVelocity(speed);
                    runner.SetPhase(RunnerPhase.Advancing);
                    if (outFt >= bagRules.OverrunFt) runner.SetOverrun(bagRules.OverrunFt, outward: false);
                    else runner.SetOverrun(outFt, outward: true);
                }
                else
                {
                    var backFt = runner.OverrunFt - speed * dt;
                    runner.SetVelocity(-speed);
                    runner.SetPhase(RunnerPhase.Returning);
                    runner.SetOverrun(backFt, outward: false);
                    if (!runner.Overrunning)
                    {
                        runner.TouchBag(1, ctx.Elapsed);
                        runner.ArrivedThisTick = true;
                        if (runner.DestBag <= 1)
                        {
                            runner.SetDest(1);
                            runner.SetPhase(RunnerPhase.OnBag);
                            runner.SetVelocity(0);
                        }
                        else
                            runner.SetPhase(RunnerPhase.Advancing);
                    }
                }
                runner.TickOnBag(dt);
                ahead = runner;
                continue;
            }

            if (held || waiting)
            {
                runner.SetVelocity(0);
                if (runner.Feet <= 0 && runner.Bag == runner.DestBag) runner.SetPhase(RunnerPhase.OnBag);
                runner.TickOnBag(dt);
                ahead = runner;
                continue;
            }

            if (runner.DestBag > runner.Bag)
            {
                var before = runner.Progress;
                var feet = runner.Feet + speed * dt;
                // No passing: stop behind the runner ahead.
                if (ahead is not null && ahead.Live)
                {
                    var limit = ahead.Progress - speedRules.NoPassFt;
                    var scale = Diamond.Baseline / runner.SegmentFt;
                    var maxProgress = Math.Max(before, limit);
                    var maxFeet = (maxProgress - runner.Bag * Diamond.Baseline) / scale;
                    if (feet > maxFeet) feet = Math.Max(runner.Feet, maxFeet);
                }
                // A lone runner cannot cross home on a fly with fewer than two outs until it resolves (§9.5).
                if (runner.Bag == 3 && ctx.Fly == FlyState.InAir && ctx.Outs < 2)
                    feet = Math.Min(feet, runner.SegmentFt - 1.0);
                runner.SetVelocity((feet - runner.Feet) / Math.Max(dt, 1e-9));
                runner.SetFeet(feet);
                var stopping = runner.DestBag == runner.Bag + 1;
                var remaining = runner.SegmentFt - runner.Feet;
                if (stopping && remaining <= bagRules.SlideFt && (runner.ForceSlide || ctx.TagThreatAt(runner.NextBag)))
                    runner.SetPhase(RunnerPhase.Sliding);
                else if (runner.Phase != RunnerPhase.Sliding || !stopping)
                    runner.SetPhase(runner.Stealing ? RunnerPhase.Stealing : RunnerPhase.Advancing);
                // Touch the bag (snap over the last half foot).
                while (runner.Live && (runner.Feet >= runner.SegmentFt || (stopping && runner.SegmentFt - runner.Feet <= bagRules.SnapFt)))
                {
                    var overflow = Math.Max(0, runner.Feet - runner.SegmentFt);
                    // The touch happened inside this frame: back out the overrun at this speed (a long scripted step is one frame).
                    var touchedAt = ctx.Elapsed - overflow / Math.Max(1e-6, speed);
                    var next = runner.Bag + 1;
                    runner.ArrivedThisTick = true;
                    if (next >= 4)
                    {
                        runner.Score(touchedAt);
                        break;
                    }
                    runner.TouchBag(next, touchedAt);
                    if (runner.DestBag <= next)
                    {
                        runner.SetFeet(0);
                        runner.SetDest(next);
                        runner.ClearForceSlide();
                        // The batter-runner runs through first (§9.4); everyone else stops on the bag.
                        if (next == 1 && runner.IsBatter && runner.Phase != RunnerPhase.Sliding)
                        {
                            runner.BeginOverrun(overflow);
                            runner.SetPhase(RunnerPhase.Advancing);
                        }
                        else
                            runner.SetPhase(RunnerPhase.OnBag);
                        break;
                    }
                    runner.SetFeet(overflow);
                    stopping = runner.DestBag == runner.Bag + 1;
                }
            }
            else if (runner.DestBag < runner.Bag || runner.Feet > 0)
            {
                // Returning (§9.3, §9.5): back down the segment, through bags when the start bag is further back.
                var feet = runner.Feet - speed * dt;
                runner.SetVelocity(-speed);
                runner.SetPhase(RunnerPhase.Returning);
                while (feet <= 0)
                {
                    if (runner.Bag > runner.DestBag && runner.Bag > 0)
                    {
                        var carry = -feet;
                        runner.RetreatToSegmentTop(runner.Bag - 1);
                        feet = runner.Feet - carry;
                        continue;
                    }
                    feet = 0;
                    runner.TouchBag(runner.Bag, ctx.Elapsed);
                    runner.SetDest(runner.Bag);
                    runner.SetPhase(RunnerPhase.OnBag);
                    runner.SetVelocity(0);
                    runner.ArrivedThisTick = true;
                    break;
                }
                runner.SetFeet(feet);
            }
            else
            {
                runner.SetVelocity(0);
                runner.SetPhase(RunnerPhase.OnBag);
            }
            runner.TickOnBag(dt);
            ahead = runner;
        }
    }
}
