using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.5 (P6, #568): steals and pickoffs, S-60 … S-72 (§4.5, §11, D2, D3, D10). The
/// steal is a body that breaks at release and the catcher's throw is the one throw model to a
/// cover on the bag; the out is the tag (§10.3). A pickoff never retires a runner on the bag and
/// catches only a runner who broke on the motion. Every assertion names the out's type, bag, and
/// runner, or the move, from the typed <see cref="PlayOutcome"/>.
/// </summary>
public sealed class StealScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanCatcher = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static readonly LiveSeats HumanRunners = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
    static readonly LiveSeats BothHuman = new(HumanBats: true, HumanPitches: true, PlayerMustField: true, Versus: true);

    // ---------------------------------------------------------------------------------
    // The jump (§11.2, D2)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheArmIsReadOffTheWindupClock()
    {
        var r = _content.Rules;
        Assert.Equal(StealArm.Set, StealBreak.ArmFor(-1, r));
        Assert.Equal(StealArm.Set, StealBreak.ArmFor(double.NaN, r));
        Assert.Equal(StealArm.Perfect, StealBreak.ArmFor(0.2, r));
        Assert.Equal(StealArm.Perfect, StealBreak.ArmFor(r.Running.Steal.PerfectWindowSec, r));
        Assert.Equal(StealArm.Windup, StealBreak.ArmFor(0.3, r));
        Assert.Equal(StealArm.None, StealBreak.ArmFor(Motion.PitchRelease, r)); // after release is too late
    }

    [Fact]
    public void TheBreakRunsAtTheAirSpeedFromReleaseAndThePerfectStealFourTenthsEarlier()
    {
        var r = _content.Rules;
        var who = _content.Must("dart");
        var speed = RunnerSystem.SpeedFtPerSec(who, 0, r);
        var air = 0.95;
        var plain = StealBreak.HeadStartFt(who, StealArm.Set, air, r);
        var perfect = StealBreak.HeadStartFt(who, StealArm.Perfect, air, r);
        Assert.Equal(speed * r.Running.Steal.AirSpeedMul * air, plain, 6);
        Assert.Equal(speed * r.Running.Steal.AirSpeedMul * (air + r.Running.Steal.PerfectEarlySec), perfect, 6);
        Assert.Equal(plain, StealBreak.HeadStartFt(who, StealArm.Windup, air, r), 6);
        Assert.Equal(0, StealBreak.HeadStartFt(who, StealArm.None, air, r));
        Assert.Equal(0, StealBreak.FeetAt(who, StealArm.Set, -0.2, r)); // nothing before release
        Assert.True(StealBreak.FeetAt(who, StealArm.Perfect, -0.2, r) > 0, "the perfect steal is off the bag before release");
        Assert.True(StealBreak.BreaksOnPickoff(StealArm.Set));
        Assert.False(StealBreak.BreaksOnPickoff(StealArm.Perfect));
        Assert.False(StealBreak.BreaksOnPickoff(StealArm.Windup));
    }

    [Fact]
    public void AnyNumberOfRunnersMayBeArmedAndTheArmDoesNotCancelTheOthers()
    {
        var match = Defense();
        Station(match, [1, 2]);
        Assert.True(match.SelectRunner(2));
        Assert.True(match.StartSteal());
        Assert.True(match.SelectRunner(1));
        Assert.True(match.CanSteal, "a steal into an armed body is the double steal (§11.1)");
        Assert.True(match.StartSteal());
        Assert.True(match.RunnerAt(1)!.StealArmed);
        Assert.True(match.RunnerAt(2)!.StealArmed);
        Assert.Equal(2, match.ArmedStealBag);
        // Stick back on one runner takes only that arm off.
        Assert.True(match.ReturnToBagAt(2));
        Assert.False(match.RunnerAt(2)!.StealArmed);
        Assert.True(match.RunnerAt(1)!.StealArmed);
    }

    [Fact]
    public void TheCoverStandsAtEveryBagOnAStealOrAPickoff()
    {
        Assert.Equal("1B", StealThrow.CoverPos(1));
        Assert.Equal("2B", StealThrow.CoverPos(2));
        Assert.Equal("3B", StealThrow.CoverPos(3));
        Assert.Equal("C", StealThrow.CoverPos(4));
        Assert.True(StealThrow.CatcherThrowSec(1, null) > 0);
        Assert.True(StealThrow.CatcherThrowSec(2, null) > StealThrow.CatcherThrowSec(1, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => StealThrow.CatcherThrowSec(0, null));
    }

    // ---------------------------------------------------------------------------------
    // S-60 … S-64  The catcher's throw play (§11.3)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S60_StraightStealOfSecondOnATakeIsTheCatchersThrowToSecondAndTheTag()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        Assert.True(match.StartSteal());
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.True(run.PitchKind is PlayKind.TakeBall or PlayKind.TakeStrike, run.PitchKind.ToString());
        Assert.True(run.Broke, "the armed runner broke on the pitch (D2)");
        var throwToSecond = Assert.Single(run.Throws.Where(t => t.Bag == 2));
        Assert.Equal("C", throwToSecond.FromPos);
        AssertStealRaceAtBag(run, runner, 2);
        // The bodies at Time ride the outcome (#574): the catcher near the plate, the cover on second.
        var bodies = run.Play.Outcome!.BodiesAtTime;
        var catcher = Assert.Single(bodies, b => b.Pos == "C");
        Assert.True(Diamond.Dist(catcher.X, catcher.Z, 0, 0) < 12, $"catcher at ({catcher.X:0},{catcher.Z:0})");
        var cover = Assert.Single(bodies, b => b.Pos == StealThrow.CoverPos(2));
        Assert.True(Diamond.Dist(cover.X, cover.Z, Diamond.Second.X, Diamond.Second.Z) <= match.Rules.Fielding.Cover.RadiusFt + 1,
            $"cover {StealThrow.CoverPos(2)} at ({cover.X:0},{cover.Z:0})");
    }

    [Fact]
    public void S61_StealOfSecondOnASwingAndMissIsTheSamePlayTheBatterDoesNotBlock()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        Assert.True(match.StartSteal());
        var whiff = Scenario.PitchAt(0.9, StrikeZoneGeometry.CenterY);
        var run = RunSteal(match, whiff, Scenario.SwingAt(14), LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.Equal(PlayKind.SwingMiss, run.PitchKind);
        Assert.Single(run.Throws.Where(t => t.Bag == 2));
        AssertStealRaceAtBag(run, runner, 2);
    }

    [Fact]
    public void S62_StrikeoutAndCaughtStealingIsTwoOutsOnOnePitchStampedDoublePlay()
    {
        var match = Defense(leadoff: "konga");
        Station(match, [1]);
        var runner = match.First!;
        var paint = Scenario.Paint;
        var take = Scenario.Take;
        Assert.False(match.BeginAtBat(paint, take, out _, out _));
        Assert.False(match.BeginAtBat(paint, take, out _, out _));
        Assert.Equal(2, match.Strikes);
        Assert.True(match.StartSteal());
        var run = RunSteal(match, paint, take, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.Equal(PlayKind.Strikeout, run.PitchKind);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal(OutType.Strikeout, outs[0].Type);
        Assert.Equal(0, outs[0].FromBag);
        // Konga (Run 2) from the bag against the gun is the tag at second: two outs on one pitch.
        Assert.Equal(2, outs.Count);
        Assert.Equal((OutType.Tag, 2, 1, runner.Id), (outs[1].Type, outs[1].Bag, outs[1].FromBag, outs[1].Runner.Id));
        Assert.Equal(PlayKind.CaughtStealing, run.Play.Kind);
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(run.Play));
        Assert.Equal(2, run.Play.OutsAfter);
        Assert.Contains("caught looking", run.Play.Caption, StringComparison.Ordinal);
        Assert.Contains("caught stealing", run.Play.Caption, StringComparison.Ordinal);
    }

    [Fact]
    public void S63_BallFourEntitlesTheRunnerFromFirstToSecondWithNoPlay()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        var wide = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = Scenario.Take;
        for (var i = 0; i < 3; i++) Assert.False(match.BeginAtBat(wide, take, out _, out _));
        Assert.Equal(3, match.Balls);
        Assert.True(match.StartSteal());
        Assert.False(match.BeginAtBat(wide, take, out _, out var walk));
        Assert.Equal(PlayKind.Walk, walk!.Kind);
        Assert.False(match.StealThrowPending, "entitled: no play on the runner (§11.3)");
        Assert.False(match.LivePlay.Active);
        Assert.Equal(runner.Id, match.Second?.Id);
        Assert.Empty(walk.Outcome!.OutsMade);
        Assert.Contains(walk.Outcome.Moves, m => m.Runner.Id == runner.Id && m.FromBag == 1 && m.ToBag == 2);
    }

    [Fact]
    public void S63b_OnAWalkTheUnforcedRunnersStealIsLive()
    {
        // Runner on second only, armed for third: ball four puts the batter on first and the steal of third is a live play.
        var match = Defense();
        Station(match, [2]);
        var runner = match.Second!;
        var wide = new PitchCommand("fastball", 0, false, AimX: 1.5);
        for (var i = 0; i < 3; i++) Assert.False(match.BeginAtBat(wide, Scenario.Take, out _, out _));
        Assert.True(match.StartSteal());
        var run = RunSteal(match, wide, Scenario.Take, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.Equal(PlayKind.Walk, run.PitchKind);
        Assert.True(run.Broke);
        Assert.Contains(run.Play.Outcome!.Moves, m => m.FromBag == 0 && m.ToBag == 1);
        AssertStealRaceAtBag(run, runner, 3);
    }

    [Fact]
    public void S64_StealOnABallInPlayIsRunningWithTheHeadStartAndForcedAnyway()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        Assert.True(match.StartSteal());
        // The pitch is thrown and hit: the armed runner broke at release (§11.2); the fixture names the batted ball.
        Assert.True(match.BeginAtBat(Scenario.Paint, Scenario.SwingAt(0), out _, out _), "the scripted swing must put the ball in play");
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var body = match.RunnerAt(1)!;
        Assert.True(body.Forced);
        Assert.True(body.Broke);
        Assert.Equal(RunnerPhase.Stealing, body.Phase);
        Assert.Equal(2, body.DestBag);
        var expected = StealBreak.HeadStartFt(runner, StealArm.Set, PitchFlight.AirSeconds(match.PitchSpeedMph(match.PreparePitch(Scenario.Paint)), match.Rules), match.Rules);
        Assert.Equal(expected, body.Feet, 3);
        Assert.True(body.Feet > 10, $"a head start of {body.Feet:0.0} ft");
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
        Assert.NotNull(play);
        // Forced anyway (S-64): whatever the play, an out on the stealer is the force at second, never a tag; otherwise they are in.
        var facts = play!.Outcome!;
        Assert.NotEmpty(facts.OutsMade);
        var onRunner = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        if (onRunner is not null) Assert.Equal((OutType.Force, 2), (onRunner.Type, onRunner.Bag));
        else Assert.Contains(facts.Moves, m => m.Runner.Id == runner.Id && m.ToBag >= 2);
    }

    // ---------------------------------------------------------------------------------
    // S-65 … S-67  Double steals and the steal of home (§11.3, D10)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S65_DoubleStealOfSecondAndThirdTheCpuCatcherPlaysTheLeadRunnerByDefault()
    {
        var match = Defense();
        Station(match, [1, 2]);
        Assert.True(match.SelectRunner(2));
        Assert.True(match.StartSteal());
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.True(run.Throws.Count > 0, "the catcher picks a bag");
        var first = run.Throws[0];
        Assert.Equal(3, first.Bag);
        var facts = run.Play.Outcome!;
        // Both bodies moved: the trailer is in at second whatever happened at third.
        Assert.Contains(facts.Moves, m => m.FromBag == 1 && m.ToBag == 2);
        Assert.True(facts.OutsMade.Count <= 1);
    }

    [Fact]
    public void S65b_DoubleStealTheHumanCatcherPicksTheTrailer()
    {
        var match = Defense();
        Station(match, [1, 2]);
        var trailer = match.First!;
        Assert.True(match.SelectRunner(2));
        Assert.True(match.StartSteal());
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        var script = new DefenseScript([2]);
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, HumanCatcher, LivePlayCommandSource.Human,
            fieldPad: (i, live) => script.Next(live, match));
        var first = run.Throws[0];
        Assert.Equal(2, first.Bag);
        AssertStealRaceAtBag(run, trailer, 2);
        Assert.Contains(run.Play.Outcome!.Moves, m => m.FromBag == 2 && m.ToBag == 3);
    }

    [Fact]
    public void S66_FirstAndThirdDelayedDoubleStealTheRunnerOnThirdBreaksAsTheThrowPassesTheMound()
    {
        // The runner on first goes; the human catcher throws to second; the offense sends the runner on
        // third once the ball is past the mound (stick); the receiver at second returns it home and the
        // tag at the plate (or the mash, §9.6) decides. Emergent from the one throw model and the runner verbs.
        var match = Defense();
        Station(match, [1, 3]);
        var third = match.Third!;
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        var sent = false;
        var script = new DefenseScript([2, 4]);
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, BothHuman, LivePlayCommandSource.Human,
            fieldPad: (i, live) => live.InClosePlay ? new LivePadInput(SouthDown: true) : script.Next(live, match),
            runPad: (i, live) =>
            {
                if (live.InClosePlay) return new LivePadInput(SouthDown: true);
                if (!sent && live.Throwing && live.ThrowBag == 2 && live.BallZ > Diamond.Rubber.Z)
                {
                    sent = true;
                    return new LivePadInput(KeysBag: 3, StickBag: 4);
                }
                return LivePadInput.Dead;
            });
        Assert.True(sent, "the throw passed the mound with the runner on third watching");
        Assert.Contains(run.Throws, t => t.Bag == 2);
        var home = run.Throws.FirstOrDefault(t => t.Bag == 4);
        var facts = run.Play.Outcome!;
        var tagged = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == third.Id);
        var scored = facts.Moves.Any(m => m.Runner.Id == third.Id && m.ToBag == 4);
        Assert.True(tagged is not null ^ scored, "the runner from third is out at the plate or scores: one of them");
        if (tagged is not null)
        {
            Assert.NotNull(home);
            Assert.Equal((OutType.Tag, 4, 3), (tagged.Type, tagged.Bag, tagged.FromBag));
        }
        else
            Assert.Equal(1, run.Play.RunsScored);
    }

    [Fact]
    public void S66b_TheCutoffVerbSendsTheThrowToTheMiddleInfielderInFrontOfSecond()
    {
        var match = Defense();
        Station(match, [1, 3]);
        Assert.True(match.SelectRunner(1));
        Assert.True(match.StartSteal());
        var cutPos = "";
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, HumanCatcher, LivePlayCommandSource.Human,
            fieldPad: (i, live) => live.HoldsBall && !live.Throwing && live.GlovePos == "C" && live.Throws == 0 && !live.Throwing
                ? new LivePadInput(Cutoff: true)
                : LivePadInput.Dead,
            observe: live =>
            {
                if (live.Throwing && live.ThrowBag == 0 && string.IsNullOrEmpty(cutPos)) cutPos = live.CoverPos;
            });
        Assert.True(cutPos is "SS" or "2B", $"the free middle infielder cuts in front of second (S-66); got '{cutPos}'");
        Assert.Contains(run.Throws, t => t.Bag == 0);
    }

    [Fact]
    public void S67_StealOfHomeIsACatcherTagAtThePlateAndNeedsAPerfectStealOnAChangeup()
    {
        // An ordinary Run-5 body from third is tagged at the plate by the catcher who already holds the ball.
        var plain = Defense();
        Station(plain, [3]);
        var walker = plain.Third!;
        Assert.True(plain.StartSteal());
        var run = RunSteal(plain, Scenario.Paint, Scenario.Take, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        var tag = Assert.Single(run.Play.Outcome!.OutsMade);
        Assert.Equal((OutType.Tag, 4, 3, walker.Id), (tag.Type, tag.Bag, tag.FromBag, tag.Runner.Id));
        Assert.Equal(PlayKind.CaughtStealing, run.Play.Kind);
        Assert.Empty(run.Throws.Where(t => t.Bag == 4 && t.FromPos == "C"));
        Assert.Equal(0, run.Play.RunsScored);

        // A Run-9 body armed inside the window, on a changeup, has the race: whichever way it goes it is the plate that decides.
        var burner = Defense(leadoff: "zig");
        Assert.True(burner.StationRunner(3, _content.Must("zig")));
        Assert.True(burner.StartSteal(windupSec: 0.2));
        Assert.Equal(StealArm.Perfect, burner.RunnerAt(3)!.StealArm);
        var slow = Scenario.PitchAt(0, StrikeZoneGeometry.CenterY, changeup: true);
        var dash = RunSteal(burner, slow, Scenario.Take, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, _) => i % 4 == 0 ? new LivePadInput(SouthDown: true) : LivePadInput.Dead);
        var facts = dash.Play.Outcome!;
        var home = facts.Moves.Any(m => m.FromBag == 3 && m.ToBag == 4);
        var outAtPlate = facts.OutsMade.Any(o => o.FromBag == 3 && o.Bag == 4 && o.Type == OutType.Tag);
        Assert.True(home ^ outAtPlate, "the run or the tag at the plate");
        if (home) Assert.Equal(1, dash.Play.RunsScored);
        Assert.True(dash.HeadStartFt > run.HeadStartFt, "the perfect steal on the slow pitch breaks further");
    }

    // ---------------------------------------------------------------------------------
    // S-68 … S-72  Pickoffs (§4.5, §11.4, D3)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S68_PickoffAtFirstWithTheRunnerOnTheBagIsTheBeatNoPlayNoStamp()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        var balls = match.Balls;
        var ev = match.Pickoff(1);
        Assert.NotNull(ev);
        Assert.Equal(PlayKind.Pickoff, ev!.Kind);
        Assert.False(PlayStamp.Shows(ev.Kind));
        Assert.Empty(ev.Outcome!.OutsMade);
        Assert.Equal(runner.Id, match.First?.Id);
        Assert.Equal(balls, match.Balls);
        Assert.Equal(0, match.Outs);
        Assert.False(match.LivePlay.Active);
        Assert.Equal(new ThrowEndpoint(ThrowOrigin.PitcherRubber, 1), ev.Outcome.ThrowEndpoint);
    }

    [Fact]
    public void S69_PickoffAtFirstOnARunnerArmedInSetCatchesThemBetweenBags()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        Assert.True(match.StartSteal());
        Assert.Equal(StealArm.Set, match.RunnerAt(1)!.StealArm);
        var run = RunPickoff(match, 1, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.True(run.Broke, "the SET arm broke on the pitcher's first motion (D3)");
        Assert.Equal(1, run.Throws[0].Bag);
        Assert.Equal("P", run.Throws[0].FromPos);
        var facts = run.Play.Outcome!;
        var tagged = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        if (tagged is not null)
        {
            Assert.Equal(OutType.Tag, tagged.Type);
            Assert.Contains(tagged.Bag, new[] { 1, 2 });
            Assert.Equal(RunnerPlayResult.PickedOff, facts.RunnerResult);
            Assert.Equal("PICKED OFF", PlayStamp.Label(run.Play));
        }
        else
        {
            // Nothing else ends a pickoff play: the body is on a bag, by geometry.
            Assert.True(match.First?.Id == runner.Id || match.Second?.Id == runner.Id);
        }
        Assert.Equal(new ThrowEndpoint(ThrowOrigin.PitcherRubber, 1), facts.ThrowEndpoint);
    }

    [Fact]
    public void S69b_APerfectArmIsTheWindupsAndAPickoffInSetFindsThemOnTheBag()
    {
        var match = Defense();
        Station(match, [1]);
        Assert.True(match.StartSteal(windupSec: 0.1));
        Assert.Equal(StealArm.Perfect, match.RunnerAt(1)!.StealArm);
        var ev = match.Pickoff(1);
        Assert.Equal(PlayKind.Pickoff, ev!.Kind);
        Assert.Empty(ev.Outcome!.OutsMade);
        Assert.True(match.RunnerAt(1)!.StealArmed, "the arm stands for the pitch");
    }

    [Theory]
    [InlineData("soot", false)]
    [InlineData("lace", true)]
    public void S70_PerfectStealAgainstTheCatcherIsTheRace(string catcherId, bool niceRelease)
    {
        // Zig (Run 9) armed 0.2 s into the windup breaks 0.4 s early (D2). Against a Field-5 CPU catcher the body
        // is in ahead of the ball; against the roster's best arm (Field 8; the spec row names 9) released at once
        // (the human's Nice release) the tag is there.
        var match = Defense(catcher: catcherId);
        Assert.True(match.StationRunner(1, _content.Must("zig")));
        Assert.True(match.StartSteal(windupSec: 0.2));
        Assert.Equal(StealArm.Perfect, match.RunnerAt(1)!.StealArm);
        var seats = niceRelease ? HumanCatcher : LiveSeats.CpuOnly;
        var source = niceRelease ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var script = new DefenseScript([2]);
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, seats, source,
            fieldPad: niceRelease ? (i, live) => script.Next(live, match) : null);
        var facts = run.Play.Outcome!;
        if (niceRelease)
        {
            var throwToSecond = Assert.Single(run.Throws.Where(t => t.Bag == 2));
            Assert.True(throwToSecond.ReleaseSec <= Frame + 1e-9, $"released at once; got {throwToSecond.ReleaseSec:0.000}");
            var tag = Assert.Single(facts.OutsMade);
            Assert.Equal((OutType.Tag, 2, 1), (tag.Type, tag.Bag, tag.FromBag));
            Assert.Equal(PlayKind.CaughtStealing, run.Play.Kind);
        }
        else
        {
            Assert.Single(run.Throws.Where(t => t.Bag == 2)); // the catcher throws on a close race rather than conceding (§8.8)
            Assert.Empty(facts.OutsMade);
            Assert.Contains(facts.Moves, m => m.FromBag == 1 && m.ToBag == 2);
            Assert.Equal(PlayKind.StolenBase, run.Play.Kind);
            Assert.Equal("STOLEN BASE", PlayStamp.Label(run.Play));
        }
    }

    [Fact]
    public void S71_APickoffThrowThatSailsIsLiveAndTheRunnerAdvances()
    {
        // Bad chemistry slants the throw past the cover (§8.5); the ball rolls loose and the body who broke takes second.
        RunResult? sailed = null;
        for (var seed = 1; seed <= 60 && sailed is null; seed++)
        {
            // Ashlord at first: bad chemistry with the pitcher on the mound (the slant is the pair's, §8.5).
            var match = Defense(seed: seed, first: "ashlord");
            Station(match, [1]);
            Assert.True(match.StartSteal());
            var run = RunPickoff(match, 1, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
            if (run.Sailed) sailed = run;
        }
        Assert.NotNull(sailed);
        var facts = sailed!.Play.Outcome!;
        Assert.Contains(facts.Moves, m => m.FromBag == 1 && m.ToBag >= 2);
        Assert.Equal(PlayKind.StolenBase, sailed.Play.Kind);
        Assert.True(facts.Error, "the sail is the ERROR, not the steal");
        Assert.Equal(PlayStamp.Error, PlayStamp.Label(sailed.Play));
    }

    [Fact]
    public void S72_TheCpuNeverPicksOffARunnerOnTheBag()
    {
        // No arm, no play: however often the CPU throws over, a runner standing on the bag is never retired (D3).
        var picked = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var match = Defense(seed: seed);
            Station(match, [1]);
            var runner = match.First!;
            var ev = match.Pickoff(1);
            Assert.Equal(PlayKind.Pickoff, ev!.Kind);
            Assert.Empty(ev.Outcome!.OutsMade);
            Assert.Equal(runner.Id, match.First?.Id);
            // The CPU game: many SETs with a runner on and no arm, no pickoff out ever.
            var before = match.Outs;
            for (var pitch = 0; pitch < 6 && match.First?.Id == runner.Id && !match.Over; pitch++)
                match.Play(new PitchCommand("fastball", 0, false, AimX: 1.5), Scenario.Take);
            picked += match.Log.Count(e => e.Outcome?.RunnerResult == RunnerPlayResult.PickedOff);
            _ = before;
        }
        Assert.Equal(0, picked);
        var chance = _content.Rules.Cpu.Active.PickoffChance;
        Assert.True(chance is > 0 and < 0.2, "the read is a rate per SET, never an out");
    }

    // ---------------------------------------------------------------------------------
    // CPU tables (§11.6, §4.5)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void CpuStealTableIsBaseByRunTimesSituationAndNeverIntoABody()
    {
        var cpu = _content.Rules.Running.Cpu;
        Assert.Equal(0, RunnerAi.StealBase(4, cpu));
        Assert.Equal(cpu.StealBaseRun6, RunnerAi.StealBase(6, cpu), 6);
        Assert.Equal(cpu.StealBaseRun8, RunnerAi.StealBase(8, cpu), 6);
        Assert.Equal(cpu.StealBaseRun10, RunnerAi.StealBase(10, cpu), 6);
        Assert.True(RunnerAi.StealBase(7, cpu) > RunnerAi.StealBase(6, cpu));

        var match = Defense(leadoff: "zig");
        Assert.True(match.StationRunner(1, _content.Must("zig")));
        Assert.True(match.StationRunner(2, _content.Must("dart")));
        var arms = 0;
        var trailingArmedBehindALead = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var plan = RunnerAi.StealPlan(match.Runners, captainUp: false, outs: 0, trailingRuns: 0, new Random(seed), match.Rules);
            arms += plan.Count;
            if (plan.Any(p => p.Runner.Bag == 2) && plan.Any(p => p.Runner.Bag == 1)) trailingArmedBehindALead++;
        }
        Assert.True(arms > 0);
        Assert.Equal(0, trailingArmedBehindALead);
        Assert.Empty(RunnerAi.StealPlan(match.Runners, false, 0, trailingRuns: cpu.StealTrailingRuns, new Random(1), match.Rules));
        // Once per at-bat, as a runner verb (§11.6): the second read of the same SET arms nothing new.
        var once = Defense(leadoff: "zig");
        Assert.True(once.StationRunner(1, _content.Must("zig")));
        var armed = once.CpuArmSteal();
        Assert.False(once.CpuArmSteal());
        _ = armed;
    }

    [Fact]
    public void CpuPickoffReadIsARateThatRisesWhenItSeesAnArmAndNeverSeesAPerfectOne()
    {
        var r = _content.Rules;
        int Reads(bool armed, double windupSec = -1)
        {
            var n = 0;
            for (var seed = 1; seed <= 300; seed++)
            {
                var match = Defense(seed: seed);
                Station(match, [1]);
                if (armed) Assert.True(match.StartSteal(windupSec));
                if (match.CpuPickoffBag() > 0) n++;
            }
            return n;
        }
        var quiet = Reads(false);
        var seen = Reads(true);
        var perfect = Reads(true, windupSec: 0.1);
        var expect = 300 * r.Cpu.Active.PickoffChance;
        Assert.InRange(quiet, expect * 0.3, expect * 2.2);
        Assert.True(seen > quiet * 1.5, $"the pip raises the read: {seen} vs {quiet}");
        Assert.InRange(perfect, expect * 0.3, expect * 2.2);
    }

    [Fact]
    public void StealPlaysReplayIdenticallyOnEverySeat()
    {
        string Stream(LivePlayCommandSource seat)
        {
            var match = Defense(leadoff: "konga");
            Station(match, [1]);
            Assert.True(match.StartSteal());
            var run = RunSteal(match, Scenario.Paint, Scenario.Take, LiveSeats.CpuOnly, seat);
            return Scenario.Fingerprint(match.Log) + run.Play.Outcome!.RunnerResult;
        }
        Assert.Equal(Stream(LivePlayCommandSource.Cpu), Stream(LivePlayCommandSource.Human));
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    sealed class ThrowRecord
    {
        public int Bag;
        public string FromPos = "";
        public double ReleaseSec;
        public bool Landed;
    }

    sealed class RunResult
    {
        public PlayEvent Play = null!;
        public PlayKind PitchKind;
        public bool Broke;
        public double HeadStartFt;
        public bool Sailed;
        public bool SawIcon;
        public List<ThrowRecord> Throws = [];
    }

    /// <summary>The steal race as the bodies decided it: the tag at the bag, or the body in — both typed, one of them.</summary>
    static void AssertStealRaceAtBag(RunResult run, Character runner, int bag)
    {
        var facts = run.Play.Outcome!;
        var tagged = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        var moved = facts.Moves.FirstOrDefault(m => m.Runner.Id == runner.Id);
        Assert.True(tagged is not null ^ moved is not null, "caught stealing or a stolen base, never both, never neither");
        if (tagged is not null)
        {
            Assert.Equal((OutType.Tag, bag, bag - 1), (tagged.Type, tagged.Bag, tagged.FromBag));
            Assert.Equal(PlayKind.CaughtStealing, run.Play.Kind);
            Assert.Equal(RunnerPlayResult.CaughtStealing, facts.RunnerResult);
            Assert.Equal("CAUGHT STEALING", PlayStamp.Label(run.Play));
        }
        else
        {
            Assert.Equal((bag - 1, bag), (moved!.FromBag, moved.ToBag));
            Assert.Equal(PlayKind.StolenBase, run.Play.Kind);
            Assert.Equal(RunnerPlayResult.StolenBase, facts.RunnerResult);
            Assert.Equal(bag, facts.RunnerToBag);
        }
        Assert.Equal(ThrowOrigin.Catcher, facts.ThrowEndpoint?.Origin);
    }

    RunResult RunSteal(Match match, PitchCommand pitch, SwingCommand swing, LiveSeats seats, LivePlayCommandSource seat,
        Func<int, LivePlaySystem, LivePadInput>? fieldPad = null, Func<int, LivePlaySystem, LivePadInput>? runPad = null,
        Action<LivePlaySystem>? observe = null)
    {
        Assert.False(match.BeginAtBat(pitch, swing, out _, out var finished), "a take or a miss");
        Assert.NotNull(finished);
        var result = new RunResult { PitchKind = finished!.Kind };
        if (!match.StealThrowPending)
        {
            result.Play = finished;
            return result;
        }
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginSteal(finished, seats, seat));
        Assert.True(live.Active);
        var broke = match.Runners.Where(r => r.Live && r.Broke).ToList();
        result.Broke = broke.Count > 0;
        result.HeadStartFt = broke.Count > 0 ? broke.Max(r => r.Feet) : 0;
        return Drive(match, result, seats, seat, fieldPad, runPad, observe);
    }

    RunResult RunPickoff(Match match, int bag, LiveSeats seats, LivePlayCommandSource seat,
        Func<int, LivePlaySystem, LivePadInput>? fieldPad = null, Func<int, LivePlaySystem, LivePadInput>? runPad = null)
    {
        var result = new RunResult();
        if (!match.BeginPickoff(bag, seats, out var dead, seat))
        {
            Assert.NotNull(dead);
            result.Play = dead!;
            result.PitchKind = dead!.Kind;
            return result;
        }
        result.PitchKind = PlayKind.Pickoff;
        result.Broke = match.Runners.Any(r => r.Live && r.Broke);
        return Drive(match, result, seats, seat, fieldPad, runPad, null);
    }

    static RunResult Drive(Match match, RunResult result, LiveSeats seats, LivePlayCommandSource seat,
        Func<int, LivePlaySystem, LivePadInput>? fieldPad, Func<int, LivePlaySystem, LivePadInput>? runPad, Action<LivePlaySystem>? observe)
    {
        var live = match.LivePlay;
        ThrowRecord? inFlight = null;
        PlayEvent? play = null;
        // Throws the play began with (the pickoff) pop before the first tick.
        if (live.Throwing)
        {
            inFlight = new ThrowRecord { Bag = live.ThrowBag, FromPos = live.ThrowFromPos, ReleaseSec = live.ElapsedSeconds };
            result.Throws.Add(inFlight);
        }
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var field = fieldPad?.Invoke(i, live) ?? LivePadInput.Dead;
            var run = runPad?.Invoke(i, live) ?? LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, field, run, false, seat));
            if (live.Events.Contains(LiveEvent.ThrowPop))
            {
                inFlight = new ThrowRecord { Bag = live.ThrowBag, FromPos = live.ThrowFromPos, ReleaseSec = live.ElapsedSeconds - Frame };
                result.Throws.Add(inFlight);
            }
            if (live.Events.Contains(LiveEvent.ThrowSailed)) result.Sailed = true;
            if (live.CloseIcon) result.SawIcon = true;
            if (inFlight is not null && !live.Throwing)
            {
                inFlight.Landed = !live.LooseBall;
                inFlight = null;
            }
            observe?.Invoke(live);
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.False(live.Active);
        result.Play = play!;
        _ = seats;
        return result;
    }

    /// <summary>The human catcher's script: a throw to a bag per press, once the ball is held.</summary>
    sealed class DefenseScript
    {
        readonly int[] _plays;
        int _index;
        bool _pending;

        public DefenseScript(int[] plays) => _plays = plays;

        public LivePadInput Next(LivePlaySystem live, Match match)
        {
            if (_pending && live.Throwing) { _pending = false; _index++; }
            if (_index >= _plays.Length) return LivePadInput.Dead;
            if (!live.HoldsBall || live.Throwing || live.LooseBall || live.InClosePlay) return LivePadInput.Dead;
            _pending = true;
            _ = match;
            return new LivePadInput(KeysBag: _plays[_index], SouthDown: true);
        }
    }

    /// <summary>
    /// The P5 fixture: Vale pitching, Pewter (Field 7) catching by default, the away order stationed as
    /// runners (the roster order is the glove diamond, P first). <paramref name="catcher"/> swaps the
    /// glove behind the plate; <paramref name="leadoff"/> is the away leadoff.
    /// </summary>
    Match Defense(string leadoff = "cinder", string catcher = "pewter", int seed = 1, string first = "lace")
    {
        var shortstop = first == "ashlord" ? "lace" : "ashlord";
        var home = _content.Team("Defense", "vale", catcher, first, "frost", "basil", shortstop, "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", leadoff, "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        var match = Match.Exhibition(_content, home, away, 3, seed);
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        Assert.Equal(catcher, map["C"].Id);
        return match;
    }

    /// <summary>Station the away order's next hitters on the bags (bag n gets order[n + 1], never the batter).</summary>
    static void Station(Match match, int[] bags)
    {
        foreach (var bag in bags)
            Assert.True(match.StationRunner(bag, match.AwayOrder[bag + 1]));
    }
}
