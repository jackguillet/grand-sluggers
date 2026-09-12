using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B rows owned by P3 (#565): the runner model (§9), Time and scoring (§10.6, §1),
/// and S-90 with runner objects. Every runner here is a body the live ball moves; nothing is
/// placed by a table.
/// </summary>
public sealed class RunnerScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanOffense = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);

    // ---------------------------------------------------------------------------------
    // S-36 … S-39  Unforced hold / go by margin, the contact play
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S36_RunnerOnSecondHoldsOnAGrounderInFrontOfThemAndTheThrowGoesToFirst()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(2, 2);
        var match = scenario.Match;
        var runner = match.Second!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 90, 5, -35);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Position is "3B" or "SS", preview.Position);
        var throwsTo = new List<int>();
        var play = Run(match, hit, preview, GroundOut(match, preview), LiveSeats.CpuOnly, live => Note(live, throwsTo));

        Assert.Equal(runner.Id, match.Second?.Id);
        Assert.DoesNotContain(play.Outcome!.Moves, m => m.Runner.Id == runner.Id);
        Assert.DoesNotContain(3, throwsTo);
        Assert.Contains(1, throwsTo);
    }

    [Theory]
    [InlineData(130, 4, 22)]   // hard, straight at the second baseman: the margin is thin
    [InlineData(110, 5, 30)]   // slower, to the first baseman: the throw across is long
    public void S37_RunnerOnSecondGoesForThirdOnAGrounderBehindThemWhenTheMarginSaysSo(double carry, double launch, double spray)
    {
        var scenario = new Scenario(_content, seed: 1).Runner(2, 2);
        var match = scenario.Match;
        var runner = match.RunnerAt(2)!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, carry, launch, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Position is "2B" or "1B", preview.Position);
        // The margin the runner reads at contact (§9.9): the fielder's route to the ball, the reaction, the throw across.
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var start = Diamond.Positions[preview.Position];
        var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z,
            FieldingResolver.ChaseSpeedFt(map[preview.Position], preview.Frozen, match.Rules), match.Rules);
        var meetAt = route.Reachable ? route.MeetTimeSec : Math.Max(BallFlight.RestTime(preview.Ball.Samples), route.TravelTimeSec);
        var ball = new BallSituation(false, false, 0, 0, route.X, route.Z, meetAt,
            FieldingResolver.OutfieldGrass(route.X, route.Z, match.Rules), preview.LandingX, preview.LandingZ, hit.CarryFt);
        var margin = RunnerAi.Margin(runner, 3, new RunnerAiContext(0, 0, int.MinValue, FlyState.None, ball, 0), match.Rules);
        var threshold = match.Rules.Running.Cpu.GroundGoMarginSec + match.Rules.Cpu.Active.RunnerMarginSec;

        var wentAtContact = false;
        var throwsTo = new List<int>();
        Run(match, hit, preview, GroundOut(match, preview), LiveSeats.CpuOnly, live =>
        {
            Note(live, throwsTo);
            if (live.ElapsedSeconds < Frame * 2 && runner.DestBag == 3) wentAtContact = true;
        });

        Assert.Equal(margin > threshold, wentAtContact);
        if (wentAtContact) Assert.Contains(3, throwsTo);
        else Assert.Equal(1, throwsTo[0]);
    }

    [Fact]
    public void S38_RunnerOnThirdHoldsOnAComebackerWithTheInfieldInAndFewerThanTwoOuts()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(3, 2).Outs(1);
        var match = scenario.Match;
        var runner = match.Third!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 40, 4, 0);
        var preview = match.PreviewHit(hit);
        var fielderFromHome = Diamond.Dist(Diamond.Positions[preview.Position].X, Diamond.Positions[preview.Position].Z, 0, 0);
        Assert.True(fielderFromHome < match.Rules.Running.Cpu.InfieldBackFt, $"{preview.Position} is in: {fielderFromHome:0} ft");
        var throwsTo = new List<int>();
        var wentBeforeTheThrow = false;
        Run(match, hit, preview, GroundOut(match, preview), LiveSeats.CpuOnly, live =>
        {
            Note(live, throwsTo);
            var third = match.RunnerAt(3);
            // Until the ball is thrown to first the runner reads a hold: the fielder is in and the margin home is not there.
            if (throwsTo.Count == 0 && third is { Live: true } && (third.DestBag == 4 || third.Feet > 0)) wentBeforeTheThrow = true;
        });

        Assert.False(wentBeforeTheThrow, "the runner on third held at contact and at the pickup (§9.9)");
        Assert.NotEmpty(throwsTo);
        Assert.Equal(1, throwsTo[0]);
        // What they do once the ball is on its way to first is the margin again, with the arm table as it stands (P4's).
        _ = runner;
    }

    [Fact]
    public void S39_RunnerOnThirdGoesOnContactWithTwoOuts()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(3, 2).Outs(2);
        var match = scenario.Match;
        var runner = match.RunnerAt(3)!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 40, 4, 0);
        var preview = match.PreviewHit(hit);
        var wentAt = -1.0;
        Run(match, hit, preview, GroundOut(match, preview), LiveSeats.CpuOnly, live =>
        {
            if (wentAt < 0 && runner.DestBag == 4) wentAt = live.ElapsedSeconds;
        });
        Assert.True(wentAt >= 0 && wentAt <= Frame * 2, $"went at {wentAt:0.00}");
    }

    // ---------------------------------------------------------------------------------
    // §9.1  One body, one speed, no passing; §9.3 per-runner verbs; §9.5 fly rules
    // ---------------------------------------------------------------------------------

    [Fact]
    public void RunnersNeverPassEachOther()
    {
        // A fast trailer behind a slow leader, both sent on a ball to the corner: the trailer waits behind.
        var fast = _content.Must("dart");
        var slow = _content.Must("konga");
        Assert.True(fast.Stats.Run > slow.Stats.Run);
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        Assert.True(match.StationRunner(1, fast));
        Assert.True(match.StationRunner(2, slow));
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 280, 10, 40);
        var preview = match.PreviewHit(hit);
        var gap = match.Rules.Running.BagSec.NoPassFt;
        var worst = double.MaxValue;
        Run(match, hit, preview, null, HumanOffense, live =>
        {
            var lead = match.RunnerAt(2);
            var trail = match.RunnerAt(1);
            if (lead is { Live: true } && trail is { Live: true })
                worst = Math.Min(worst, lead.Progress - trail.Progress);
        }, runPad: new LivePadInput(AllAdvance: true));
        Assert.True(worst >= gap - 1.0, $"closest gap {worst:0.0} ft");
    }

    [Fact]
    public void PerRunnerSendAndHaltMoveOneBodyAndForcedRunnersCannotBeHeld()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1).Runner(3, 3);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 200, 6, 15);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder, preview.Class.ToString());
        var forcedMoved = false;
        var thirdMoved = false;
        var frame = 0;
        Run(match, hit, preview, null, HumanOffense, live =>
        {
            frame++;
            var first = match.RunnerAt(1);
            var third = match.RunnerAt(3);
            if (first is { Live: true } && frame < 30 && first.Feet > 0) forcedMoved = true;
            if (third is { Live: true } && frame < 90 && third.Feet > 0) thirdMoved = true;
        }, pad: frame =>
        {
            // Freeze everyone at contact: the forced runner from first goes anyway; the runner on third stays.
            if (frame < 30) return new LivePadInput(Freeze: true);
            // Then select the runner on third and send them by the stick toward home.
            if (frame < 90) return new LivePadInput(KeysBag: 3, StickBag: 4);
            return LivePadInput.Dead;
        });
        Assert.True(forcedMoved, "a forced runner is not held (§9.3): the freeze at contact does not stop them");
        Assert.True(thirdMoved, "the stick toward home sent the runner from third");
    }

    [Fact]
    public void AFlyHoldsEveryRunnerUntilTheCatchThenTheThirdBaseRunnerTags()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1).Runner(3, 3);
        var match = scenario.Match;
        var onThird = match.Third!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 260, 34, 0);
        var preview = match.PreviewHit(hit);
        var field = FlyOut(match, preview);
        var leftEarly = false;
        var leftAt = -1.0;
        var caughtAt = -1.0;
        var play = Run(match, hit, preview, field, LiveSeats.CpuOnly, live =>
        {
            var third = match.RunnerAt(3);
            if (caughtAt < 0 && (live.Caught || live.Events.Contains(LiveEvent.Glove))) caughtAt = live.ElapsedSeconds;
            if (live.Fly == FlyState.InAir && third is { Live: true } && third.Feet > 0) leftEarly = true;
            if (leftAt < 0 && third is { Live: true } && third.Feet > 0) leftAt = live.ElapsedSeconds;
        });
        Assert.False(leftEarly, "nobody leaves on a catchable fly (§9.5)");
        Assert.True(caughtAt > 0 && leftAt >= caughtAt - Frame, $"left at {leftAt:0.00} vs the catch {caughtAt:0.00}");
        Assert.Contains(play.Outcome!.OutsMade, o => o.Type == OutType.Catch);
        var tagged = play.Outcome.Moves.Any(m => m.Runner.Id == onThird.Id && m.ToBag == 4)
                     || play.Outcome.OutsMade.Any(o => o.Runner.Id == onThird.Id);
        Assert.True(tagged, "the runner on third tags on a deep fly with fewer than two outs");
    }

    [Fact]
    public void AllAdvanceBeforeTheCatchIsTagAndGo()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var runner = match.First!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 240, 34, 0);
        var preview = match.PreviewHit(hit);
        var leftEarly = false;
        var left = false;
        var play = Run(match, hit, preview, FlyOut(match, preview), HumanOffense, live =>
        {
            var first = match.RunnerAt(1);
            if (first is not { Live: true }) return;
            if (live.Fly == FlyState.InAir && first.Feet > 0) leftEarly = true;
            if (live.Fly == FlyState.Caught && first.Feet > 0) left = true;
        }, runPad: new LivePadInput(AllAdvance: true));
        Assert.False(leftEarly, "LB before the catch waits on the bag");
        Assert.True(left, "and leaves at the catch");
        Assert.Null(match.First);
        Assert.True(match.Second?.Id == runner.Id || play.Outcome!.OutsMade.Any(o => o.Runner.Id == runner.Id));
    }

    [Fact]
    public void AStickSendBeforeTheCatchLeavesEarlyAndOwesARetouch()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 240, 34, 0);
        var preview = match.PreviewHit(hit);
        var leftEarly = false;
        var owed = false;
        Run(match, hit, preview, FlyOut(match, preview), HumanOffense, live =>
        {
            var first = match.RunnerAt(1);
            if (first is not { Live: true }) return;
            if (live.Fly == FlyState.InAir && first.Feet > 0) leftEarly = true;
            if (live.Fly == FlyState.Caught && first.LeftEarly) owed = true;
        }, pad: frame => frame < 30 ? new LivePadInput(KeysBag: 1, StickBag: 2) : LivePadInput.Dead);
        Assert.True(leftEarly, "a human send overrides the fly hold");
        Assert.True(owed, "off the bag at the catch: must retouch (§10.5)");
    }

    [Fact]
    public void ALoneRunnerCannotCrossHomeOnAFlyUntilItResolves()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(3, 3);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 300, 34, 0);
        var preview = match.PreviewHit(hit);
        var crossedInAir = false;
        var heldShort = false;
        Run(match, hit, preview, FlyOut(match, preview), HumanOffense, live =>
        {
            var third = match.RunnerAt(3);
            if (third is null) return;
            if (live.Fly == FlyState.InAir && third.Scored) crossedInAir = true;
            if (live.Fly == FlyState.InAir && third.Live && third.Feet >= third.SegmentFt - 1.5) heldShort = true;
        }, pad: frame => frame < 30 ? new LivePadInput(KeysBag: 3, StickBag: 4) : LivePadInput.Dead);
        Assert.False(crossedInAir, "a lone runner cannot cross home on a fly with fewer than two outs (§9.5)");
        Assert.True(heldShort, "the body waits a step short of the plate");
    }

    // ---------------------------------------------------------------------------------
    // S-77 … S-79  Time and the runs that count on the third out
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S77_SingleWithRunnersOnFirstAndThirdScoresOneAndTimeWaitsForTheBodies()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1).Runner(3, 3);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 260, 6, 8);
        var preview = match.PreviewHit(hit);
        var lastSettled = -1.0;
        var play = Run(match, hit, preview, null, LiveSeats.CpuOnly, live =>
        {
            foreach (var r in match.Runners)
                if (r.Live && r.OnBag && r.OnBagSec < Frame * 1.5 && live.ElapsedSeconds > lastSettled) lastSettled = live.ElapsedSeconds;
        }, out var doneAt);
        // The runner from third scores; how far the runner from first gets is the arm table's (fielding.throw, P4's).
        Assert.True(play.RunsScored >= 1, $"runs {play.RunsScored}");
        Assert.Contains(play.Outcome!.Moves, m => m.FromBag == 3 && m.ToBag == 4);
        Assert.True(doneAt - lastSettled >= match.Rules.Running.Bags.TimeOnBagSec - Frame * 2,
            $"Time came {doneAt - lastSettled:0.00} s after the last body settled");
    }

    [Fact]
    public void S78_ARunThatCrossedBeforeAForceThirdOutDoesNotCount()
    {
        var order = new Scenario(_content, seed: 1).Match.AwayOrder;
        var slow = Enumerable.Range(1, order.Count - 1).OrderBy(i => order[i].Stats.Run).First();
        var fast = Enumerable.Range(1, order.Count - 1).OrderByDescending(i => order[i].Stats.Run).First();
        var scenario = new Scenario(_content, seed: 1).Runner(1, slow).Runner(3, fast).Outs(2);
        var match = scenario.Match;
        var onThird = match.Third!;
        var hit = scenario.Contact();
        var field = Grounder(match);
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut, LivePlayCommandSource.Cpu));
        // The contact play with two outs: the runner from third goes on contact and crosses before the force lands.
        var homeAt = RunnerSystem.ArrivalSec(match.RunnerAt(3)!, 4, 0, 0, match.Rules);
        var secondAt = RunnerSystem.ArrivalSec(match.RunnerAt(1)!, 2, 0, 0, match.Rules);
        Assert.True(homeAt < secondAt, $"the run crosses ({homeAt:0.00}) before the forced runner reaches second ({secondAt:0.00})");
        var snapshot = match.LivePlay.Apply(LivePlayCommand.Advance((homeAt + secondAt) / 2, PlayKind.GroundOut, true, false, true, 0, LivePlayCommandSource.Cpu)).Snapshot;
        Assert.Contains(snapshot.Runners, r => r.FromBag == 3 && r.Phase == RunnerPhase.Scored);
        var force = match.LivePlay.Apply(LivePlayCommand.ThrowArrived(2, runnerBeats: false, field.Fielder, LivePlayCommandSource.Cpu)).Throw;
        Assert.True(force!.Value.Out && force.Value.Force);
        var play = match.LivePlay.Apply(LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field, LivePlayCommandSource.Cpu)).CompletedPlay!;
        Assert.Equal(0, play.RunsScored);
        Assert.DoesNotContain(play.Outcome!.Moves, m => m.Runner.Id == onThird.Id && m.ToBag == 4);
        Assert.Equal(0, match.AwayScore);
    }

    [Fact]
    public void S79_ARunThatCrossedBeforeATagThirdOutCounts()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(2, 2).Runner(3, 3).Outs(2);
        var match = scenario.Match;
        var onThird = match.Third!;
        var hit = scenario.Contact();
        var field = Grounder(match);
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut, LivePlayCommandSource.Cpu));
        var snapshot = match.LivePlay.Apply(LivePlayCommand.Advance(3.6, PlayKind.GroundOut, true, false, true, 0, LivePlayCommandSource.Cpu)).Snapshot;
        Assert.Contains(snapshot.Runners, r => r.FromBag == 3 && r.Phase == RunnerPhase.Scored);
        var tag = match.LivePlay.Apply(LivePlayCommand.TagRunner(2, field.Fielder, LivePlayCommandSource.Cpu));
        Assert.Equal(2, tag.TaggedFromBag);
        var play = match.LivePlay.Apply(LivePlayCommand.Complete(Scenario.Paint, Scenario.Swing, hit, field, LivePlayCommandSource.Cpu)).CompletedPlay!;
        Assert.Equal(1, play.RunsScored);
        Assert.Contains(play.Outcome!.Moves, m => m.Runner.Id == onThird.Id && m.ToBag == 4);
        var third = Assert.Single(play.Outcome.OutsMade);
        Assert.Equal(OutType.Tag, third.Type);
        Assert.Equal(1, match.AwayScore);
        Assert.False(match.Top, "three outs: the half turned over");
    }

    // ---------------------------------------------------------------------------------
    // S-90  Runner objects replay identically
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(LivePlayCommandSource.Cpu)]
    [InlineData(LivePlayCommandSource.Human)]
    public void S90_TheSameRecordedPadsMoveTheSameBodiesTwice(LivePlayCommandSource seat)
    {
        var a = RunScripted(seat);
        var b = RunScripted(seat);
        Assert.Equal(a.Stream, b.Stream);
        Assert.Equal(a.Positions, b.Positions);
    }

    (string Stream, string Positions) RunScripted(LivePlayCommandSource seat)
    {
        var scenario = new Scenario(_content, seed: 3).Runner(1, 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 230, 10, 30);
        var preview = match.PreviewHit(hit);
        var positions = new System.Text.StringBuilder();
        Run(match, hit, preview, null, HumanOffense, live =>
        {
            foreach (var r in match.Runners)
                positions.Append($"{r.Who.Id}:{r.Bag}:{r.Feet:0.00};");
        }, pad: frame => frame is > 10 and < 40 ? new LivePadInput(AllAdvance: true) : LivePadInput.Dead, seat: seat);
        return (scenario.Stream(), positions.ToString());
    }

    // ---------------------------------------------------------------------------------

    static void Note(LivePlaySystem live, List<int> throwsTo)
    {
        if (live.Throwing && (throwsTo.Count == 0 || throwsTo[^1] != live.ThrowBag)) throwsTo.Add(live.ThrowBag);
    }

    static FieldingResult GroundOut(Match match, FieldingPreview preview)
    {
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        map.TryGetValue("2B", out var cut);
        return new FieldingResult(PlayKind.GroundOut, preview.Fielder, cut, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false,
            new ThrowResult(Chemistry.Neutral, 1.0, false));
    }

    static FieldingResult FlyOut(Match match, FieldingPreview preview) =>
        new(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);

    static FieldingResult Grounder(Match match) => new(
        PlayKind.GroundOut, match.Pitcher, match.Batter, 1.5, 48, 72, false, false,
        new ThrowResult(Chemistry.Good, 1.7, false));

    static PlayEvent Run(Match match, AtBatResult hit, FieldingPreview preview, FieldingResult? field, LiveSeats seats,
        Action<LivePlaySystem>? observe, LivePadInput? runPad = null, Func<int, LivePadInput>? pad = null,
        LivePlayCommandSource seat = LivePlayCommandSource.Cpu) =>
        Run(match, hit, preview, field, seats, observe, out _, runPad, pad, seat);

    static PlayEvent Run(Match match, AtBatResult hit, FieldingPreview preview, FieldingResult? field, LiveSeats seats,
        Action<LivePlaySystem>? observe, out double doneAt, LivePadInput? runPad = null, Func<int, LivePadInput>? pad = null,
        LivePlayCommandSource seat = LivePlayCommandSource.Cpu)
    {
        var live = match.LivePlay;
        var cpuField = field ?? match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, cpuField, seats, 0, seat)).Snapshot.Active);
        PlayEvent? play = null;
        doneAt = -1;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var run = pad?.Invoke(i) ?? runPad ?? LivePadInput.Dead;
            var result = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, run, false, seat));
            observe?.Invoke(live);
            play = result.CompletedPlay;
            if (play is not null) doneAt = match.LivePlay.ElapsedSeconds > 0 ? match.LivePlay.ElapsedSeconds : (i + 1) * Frame;
        }
        Assert.NotNull(play);
        return play!;
    }
}
