using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// R3 (#650): a play's trace contains the geometric reason — glove vs ball, runner vs bag —
/// not only the caption. Cousin of S-90; the dump is observation and must not retune
/// <c>data/rules/</c> or change a <see cref="PlayEvent"/>.
/// </summary>
public sealed class PlayTraceTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    [Fact]
    public void GrounderTrace_GloveMeetsBallAndRunnerVsBagIsTheOut()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var hit = Grounder(scenario.Contact(), match, sprayDeg: -12);
        var preview = match.PreviewHit(hit);
        var field = SyntheticGroundOut(match, preview);
        var (play, trace) = RunTraced(match, hit, preview, field);

        Assert.Equal(PlayKind.GroundOut, play.Kind);
        var outs = play.Outcome!.OutsMade;
        Assert.NotEmpty(outs);
        Assert.DoesNotContain(outs, o => o.Type == OutType.Catch);
        // Caption is presentation; the out is a force or a throw at a bag.
        Assert.Contains(outs, o => o.Type is OutType.Force or OutType.ThrowOutAtFirst);
        Assert.True(trace.Ticks.Count > 1, "a live grounder has more than the opening snapshot");
        AssertMonotonic(trace);

        var met = trace.Ticks.FirstOrDefault(t => t.Ball.Held);
        Assert.NotNull(met);
        Assert.True(Diamond.Dist(met!.Glove.X, met.Glove.Z, met.Ball.X, met.Ball.Z) < 1,
            $"glove and ball must meet; glove ({met.Glove.X:0.0},{met.Glove.Z:0.0}) ball ({met.Ball.X:0.0},{met.Ball.Z:0.0})");

        var decided = outs[0];
        var bag = Diamond.Bag(decided.Bag);
        var occupy = match.Rules.Running.Bags.TagSafeRadiusFt;
        var atBag = trace.Ticks.LastOrDefault(t =>
            Diamond.Dist(t.Ball.X, t.Ball.Z, bag.X, bag.Z) <= match.Rules.Fielding.Cover.RadiusFt + 1);
        Assert.NotNull(atBag);
        var runner = atBag!.Runners.FirstOrDefault(r => r.Id == decided.Runner.Id);
        if (runner is not null)
        {
            var runnerToBag = Diamond.Dist(runner.X, runner.Z, bag.X, bag.Z);
            Assert.True(runnerToBag > occupy,
                $"the out is the runner short of bag {decided.Bag}: runner {runnerToBag:0.0} ft, occupy {occupy}");
        }
        else
            Assert.True(trace.Ticks.Any(t => t.Runners.Any(r => r.Id == decided.Runner.Id)),
                "the retired runner was on the path before the out");
        Assert.NotNull(trace.Completed);
        Assert.Equal(play.Kind, trace.Completed!.Kind);
        Assert.Equal(outs.Select(o => (o.Type, o.Bag, o.FromBag, o.Runner.Id)),
            trace.Completed.Outs.Select(o => (o.Type, o.Bag, o.From, o.Runner)));
    }

    [Fact]
    public void FlyTrace_CatchIsARadiusAtTheWindowNotACaption()
    {
        var scenario = new Scenario(_content, seed: 2);
        var match = scenario.Match;
        var hit = Fly(scenario.Contact(), match);
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX,
            preview.LandingZ, false, false);
        var (play, trace) = RunTraced(match, hit, preview, field);

        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.Catch, 0), (only.Type, only.FromBag));
        AssertMonotonic(trace);

        var plant = FlyCatch.ChaseTarget(preview, match.Park, match.Rules);
        var window = FieldingResolver.CatchWindowFt(preview.CatchRadius, false, false, match.Rules);
        var catchTick = trace.Ticks.FirstOrDefault(t => t.Ball.Caught || t.Glove.HasBall);
        Assert.NotNull(catchTick);
        Assert.True(
            FlyCatch.JumpWindow(catchTick!.T, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules)
            || FlyCatch.JumpWindow(catchTick.T - Frame, preview.HangTimeSec, preview.Fielder, match.Park, match.Rules),
            $"caught at t={catchTick.T:0.00} vs hang {preview.HangTimeSec:0.00}");
        Assert.True(
            FlyCatch.Under(catchTick.Glove.X, catchTick.Glove.Z, catchTick.Ball.X, catchTick.Ball.Z,
                plant.X, plant.Z, window, FlyCatch.NeedsJump(preview), match.Rules),
            $"catch is the radius at the plant, not the caption; glove ({catchTick.Glove.X:0.0},{catchTick.Glove.Z:0.0}) plant ({plant.X:0.0},{plant.Z:0.0}) window {window:0.0}");
        Assert.Equal(OutType.Catch, trace.Completed!.Outs[0].Type);
    }

    [Fact]
    public void TagTrace_RunnerAndGloveAtTheBag()
    {
        var match = StealDefense();
        Assert.True(match.StationRunner(3, match.AwayOrder[4]));
        var runner = match.Third!;
        Assert.True(match.StartSteal());
        match.LivePlay.Recording = true;
        Assert.False(match.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var pitch));
        Assert.True(match.StealThrowPending);
        match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch!, LiveSeats.CpuOnly));
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
            play = match.LivePlay.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
        Assert.NotNull(play);
        var trace = match.LivePlay.TakeTrace(play);

        var tag = Assert.Single(play!.Outcome!.OutsMade);
        Assert.Equal((OutType.Tag, 4, 3, runner.Id), (tag.Type, tag.Bag, tag.FromBag, tag.Runner.Id));
        Assert.Equal(PlayKind.CaughtStealing, play.Kind);
        AssertMonotonic(trace);

        var plate = Diamond.Home;
        var reach = InPlay.TagReachFt(null, sliding: false, match.Rules);
        var tagTick = trace.Ticks.LastOrDefault(t =>
        {
            var body = t.Runners.FirstOrDefault(r => r.Id == runner.Id);
            if (body is null) return t.Glove.HasBall && Diamond.Dist(t.Glove.X, t.Glove.Z, plate.X, plate.Z) < 12;
            return t.Glove.HasBall
                   && Diamond.Dist(t.Glove.X, t.Glove.Z, body.X, body.Z) <= reach + 1
                   && Diamond.Dist(body.X, body.Z, plate.X, plate.Z) <= reach + 6;
        });
        Assert.NotNull(tagTick);
        Assert.True(tagTick!.Glove.HasBall, "the tag is a glove that has the ball");
        var tagged = tagTick.Runners.FirstOrDefault(r => r.Id == runner.Id);
        if (tagged is not null)
        {
            Assert.True(Diamond.Dist(tagTick.Glove.X, tagTick.Glove.Z, tagged.X, tagged.Z) <= reach + 1,
                $"glove-runner {Diamond.Dist(tagTick.Glove.X, tagTick.Glove.Z, tagged.X, tagged.Z):0.0} vs reach {reach}");
            Assert.True(Diamond.Dist(tagged.X, tagged.Z, plate.X, plate.Z) < 20,
                "the tag is at the plate, not a caption");
        }
        Assert.Equal(OutType.Tag, trace.Completed!.Outs[0].Type);
        Assert.Equal(4, trace.Completed.Outs[0].Bag);
    }

    [Fact]
    public void StealTrace_BreakAtReleaseAndPickoffOnlyIfAlreadyBroke()
    {
        // Break at release (D2 / D3): armed in SET, still on the bag; the pitch's first motion
        // (release on a take) is the break. The live steal play then shows the body off first.
        var steal = StealDefense();
        Assert.True(steal.StationRunner(1, steal.AwayOrder[2]));
        var who = steal.First!;
        Assert.True(steal.StartSteal());
        Assert.Equal(StealArm.Set, steal.RunnerAt(1)!.StealArm);
        Assert.False(steal.RunnerAt(1)!.Broke);
        Assert.True(InPlay.OnThisBag(1, steal.RunnerAt(1)!.Position.X, steal.RunnerAt(1)!.Position.Z,
            steal.Rules.Running.Bags.OccupyRadiusFt, steal.Rules));
        steal.LivePlay.Recording = true;
        Assert.False(steal.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var pitch));
        Assert.True(steal.RunnerAt(1)!.Broke, "the armed runner broke at release (D2)");
        Assert.True(steal.StealThrowPending);
        steal.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch!, LiveSeats.CpuOnly));
        PlayEvent? stealPlay = null;
        for (var i = 0; i < 60 * 12 && stealPlay is null; i++)
            stealPlay = steal.LivePlay.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
        Assert.NotNull(stealPlay);
        var stealTrace = steal.LivePlay.TakeTrace(stealPlay);
        Assert.True(stealPlay!.Kind is PlayKind.CaughtStealing or PlayKind.StolenBase, stealPlay.Kind.ToString());
        AssertMonotonic(stealTrace);
        var first = stealTrace.Ticks[0];
        var body = Assert.Single(first.Runners, r => r.Id == who.Id);
        Assert.True(body.Broke);
        Assert.True(body.Dest == 2 || body.From == 1);
        Assert.True(Diamond.Dist(body.X, body.Z, Diamond.First.X, Diamond.First.Z) > steal.Rules.Running.Bags.TagSafeRadiusFt,
            "the break put the body off the bag; a pickoff of a runner still on it is the beat, not this play");

        // Pickoff of a runner on the bag (never broke): the beat, no live play, no out (S-68, D3).
        var onBag = StealDefense();
        Assert.True(onBag.StationRunner(1, onBag.AwayOrder[2]));
        onBag.Tracing = true;
        var beat = onBag.Pickoff(1);
        Assert.NotNull(beat);
        Assert.Equal(PlayKind.Pickoff, beat!.Kind);
        Assert.Empty(beat.Outcome!.OutsMade);
        Assert.Equal(onBag.AwayOrder[2].Id, onBag.First?.Id);
        var beatTrace = Assert.Single(onBag.Traces);
        Assert.Empty(beatTrace.Ticks);
        Assert.Equal(PlayKind.Pickoff, beatTrace.Completed!.Kind);
        Assert.Empty(beatTrace.Completed.Outs);

        // Pickoff of a SET arm: the motion is the break (D3). The trace shows Broke, not a caption.
        var setArm = StealDefense();
        Assert.True(setArm.StationRunner(1, setArm.AwayOrder[2]));
        Assert.True(setArm.StartSteal());
        Assert.Equal(StealArm.Set, setArm.RunnerAt(1)!.StealArm);
        Assert.True(StealBreak.BreaksOnPickoff(StealArm.Set));
        Assert.False(StealBreak.BreaksOnPickoff(StealArm.Perfect));
        setArm.Tracing = true;
        var pick = setArm.Pickoff(1);
        Assert.NotNull(pick);
        var pickTrace = Assert.Single(setArm.Traces);
        if (pick!.Kind == PlayKind.Pickoff)
        {
            Assert.Empty(pick.Outcome!.OutsMade);
            Assert.Empty(pickTrace.Ticks);
        }
        else
        {
            Assert.True(pickTrace.Ticks.Count > 0);
            Assert.Contains(pickTrace.Ticks[0].Runners, r => r.Broke);
            Assert.True(
                pick.Outcome!.OutsMade.Any(o => o.Type == OutType.Tag)
                || pick.Outcome.Error
                || pick.Kind is PlayKind.StolenBase or PlayKind.CaughtStealing,
                pick.Kind.ToString());
        }
    }

    [Fact]
    public void TracingDoesNotChangeAPlayEventStream()
    {
        var plain = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 7);
        plain.AutoPlayGame();
        var traced = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 7);
        traced.Tracing = true;
        traced.AutoPlayGame();
        Assert.Equal(Scenario.Fingerprint(plain.Log), Scenario.Fingerprint(traced.Log));
        Assert.Equal(traced.Log.Count, traced.Traces.Count);
        Assert.Contains(traced.Traces, t => t.Ticks.Count > 0);
        Assert.All(traced.Traces, t => Assert.NotNull(t.Completed));
        Assert.All(traced.Traces.Where(t => t.Ticks.Count > 0), t =>
        {
            Assert.Equal(4, t.Ticks[0].Bags.Count);
            Assert.False(string.IsNullOrEmpty(t.Ticks[0].Glove.Pos));
        });
        var json = traced.TraceLog().ToJson();
        Assert.Contains("\"ball\"", json, StringComparison.Ordinal);
        Assert.Contains("\"glove\"", json, StringComparison.Ordinal);
        Assert.Contains("\"runners\"", json, StringComparison.Ordinal);
        Assert.Contains("\"bags\"", json, StringComparison.Ordinal);
        var parsed = PlayTraceLog.Parse(json);
        Assert.Equal(7, parsed.Seed);
        Assert.Equal(traced.Traces.Count, parsed.Plays.Count);
        Assert.Equal(traced.Traces.Count(t => t.Ticks.Count > 0), parsed.Plays.Count(t => t.Ticks.Count > 0));
    }

    [Fact]
    public void RecordingOffDumpsNothing()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        var hit = Grounder(scenario.Contact(), match, sprayDeg: -12);
        var preview = match.PreviewHit(hit);
        Assert.False(match.LivePlay.Recording);
        match.LivePlay.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview,
            match.ResolveFielding(hit, preview), LiveSeats.CpuOnly));
        match.LivePlay.Apply(LivePlayCommand.Tick(Frame));
        var dump = match.LivePlay.TakeTrace();
        Assert.Empty(dump.Ticks);
        Assert.Null(dump.Completed);
    }

    // ---------------------------------------------------------------------------------

    (PlayEvent Play, PlayTrace Trace) RunTraced(Match match, AtBatResult hit, FieldingPreview preview, FieldingResult? field)
    {
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly))
            .Snapshot.Active);
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 20 && play is null; i++)
            play = live.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
        Assert.NotNull(play);
        return (play!, live.TakeTrace(play));
    }

    static void AssertMonotonic(PlayTrace trace)
    {
        Assert.Equal(Enumerable.Range(0, trace.Ticks.Count), trace.Ticks.Select(t => t.I));
        for (var i = 1; i < trace.Ticks.Count; i++)
            Assert.True(trace.Ticks[i].T + 1e-9 >= trace.Ticks[i - 1].T, $"tick {i} went backwards");
        Assert.All(trace.Ticks, t => Assert.Equal(4, t.Bags.Count));
    }

    Match StealDefense(int seed = 1)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", "cinder", "dart", "jester", "grit", "soot", "boom", "nugget", "pip");
        var match = Match.Exhibition(_content, home, away, 3, seed);
        Assert.Equal("pewter", FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves)["C"].Id);
        return match;
    }

    static AtBatResult Grounder(AtBatResult hit, Match match, double sprayDeg) =>
        Shape(hit, match, exit: 84, launch: 8, spray: sprayDeg);

    static AtBatResult Fly(AtBatResult hit, Match match) => Shape(hit, match, exit: 92, launch: 34, spray: 4);

    static AtBatResult Shape(AtBatResult hit, Match match, double exit, double launch, double spray)
    {
        var carry = BallFlight.CarryFeet(exit, launch, match.Park.WindMph, match.Rules);
        return hit with
        {
            ExitVeloMph = exit,
            LaunchDeg = launch,
            SprayDeg = spray,
            CarryFt = carry,
            HomeRun = false,
            Foul = false,
            InPlay = true
        };
    }

    static FieldingResult SyntheticGroundOut(Match match, FieldingPreview preview)
    {
        var map = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
        map.TryGetValue("2B", out var cut);
        return new FieldingResult(PlayKind.GroundOut, preview.Fielder, cut, preview.HangTimeSec, preview.LandingX,
            preview.LandingZ, false, false, new ThrowResult(Chemistry.Good, 1.35, false));
    }
}
