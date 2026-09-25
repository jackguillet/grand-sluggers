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
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanCatcher = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static readonly LiveSeats HumanRunners = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
    static readonly LiveSeats BothHuman = new(HumanBats: true, HumanPitches: true, PlayerMustField: true, Versus: true);

    // ---------------------------------------------------------------------------------
    // The jump (§11.2, D2)
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(-1)] [InlineData(.2)] [InlineData(.3)] [InlineData(1)]
    public void ADepartureTimestampNeverGrantsRetroactiveMovement(double when)
    {
        var match = Defense();
        Station(match, [1]);
        Assert.True(match.StartSteal(windupSec: when));
        Assert.True(match.RunnerAt(1)!.Broke);
        Assert.Equal(0, match.RunnerAt(1)!.Feet);
        match.PitchSetup.Advance(.1);
        Assert.Equal(RunnerSystem.SpeedFtPerSec(match.First!, match.Rules, 0) * .1, match.RunnerAt(1)!.Feet, 6);
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
        Assert.Equal(2, match.RunnerAt(2)!.DestBag);
        Assert.True(match.RunnerAt(1)!.StealArmed);
    }

    [Fact]
    public void TheCoverStandsAtEveryBagOnAStealOrAPickoff()
    {
        Assert.Equal("1B", StealThrow.CoverPos(1));
        Assert.Equal("2B", StealThrow.CoverPos(2));
        Assert.Equal("3B", StealThrow.CoverPos(3));
        Assert.Equal("C", StealThrow.CoverPos(4));
        Assert.True(StealThrow.CatcherThrowSec(1, null, rules: Rules.Default) > 0);
        Assert.True(StealThrow.CatcherThrowSec(2, null, rules: Rules.Default) > StealThrow.CatcherThrowSec(1, null, rules: Rules.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => StealThrow.CatcherThrowSec(0, null, rules: Rules.Default));
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
        var throwToSecond = Assert.Single(run.Throws, t => t.Bag == 2);
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
        var whiff = Scenario.PitchAt(0.9, StrikeZoneGeometry.Reference.CenterY);
        var run = RunSteal(match, whiff, Scenario.SwingAt(14), LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.Equal(PlayKind.SwingMiss, run.PitchKind);
        Assert.Single(run.Throws, t => t.Bag == 2);
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
        match.PitchSetup.ReleaseBall();
        match.PitchSetup.Advance(PitchFlight.AirSeconds(match.PitchSpeedMph(match.PreparePitch(Scenario.Paint)), match.Rules));
        var expectedFeet = match.RunnerAt(1)!.Feet;
        // Contact preserves the actual departure, not a recalculated head start.
        Assert.True(match.BeginAtBat(Scenario.Paint, Scenario.SwingAt(0), out _, out _), "the scripted swing must put the ball in play");
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var body = match.RunnerAt(1)!;
        Assert.True(body.Forced);
        Assert.True(body.Broke);
        Assert.Equal(RunnerPhase.Stealing, body.Phase);
        Assert.Equal(2, body.DestBag);
        Assert.Equal(expectedFeet, body.Feet, 3);
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
        match.LivePlay.Recording = true;
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
        var trace = match.LivePlay.TakeTrace(run.Play);
        var release = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.ThrowRelease && m.Bag == 0);
        var reception = Assert.Single(trace.Marks!, m => m.Kind == PlayTraceMarkKind.Reception && m.Leg == release.Leg);
        Assert.True(reception.T > release.T);
        Assert.True(reception.Geometry!.ReceiverInReach);
        Assert.Equal(cutPos, release.Flight!.ReceiverPos);
    }

    // ---------------------------------------------------------------------------------
    // S-99 on the runner play (§11.3, §8.9, #637): Select is one rule for every play
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S99_OnAStealSelectWithTheBallInTheCatchersGloveMovesNothingAndTheThrowGoesWhereTheDPadSays()
    {
        // The human catcher holds the ball; Select with the stick at the pitcher (and dead) for twelve frames is refused:
        // the ring stays on the catcher and the ball in their glove. The throw then goes to the armed bag and the race is
        // decided exactly as it is without the presses — the same match, seed, stick and script, Select the only difference.
        const int presses = 12;
        var catcherSpot = StealThrow.CatcherSpot(_content.Rules);

        (RunResult Run, List<(string Pos, double GX, double GZ, double BX, double BZ)> Trace, int Presses) Play(bool select)
        {
            var match = Defense();
            Station(match, [1]);
            Assert.True(match.StartSteal());
            var script = new DefenseScript([2]);
            var trace = new List<(string, double, double, double, double)>();
            var pressed = 0;
            var run = RunSteal(match, Scenario.Paint, Scenario.Take, HumanCatcher, LivePlayCommandSource.Human,
                fieldPad: (i, live) =>
                {
                    if (i >= presses) return script.Next(live, match);
                    Assert.True(live.HoldsBall && !live.Throwing && live.GlovePos == "C", "the catcher holds the ball for every press");
                    if (select) pressed++;
                    // Even frames point the stick at the pitcher (the stick walks the catcher too, §8.1); odd frames are a dead stick.
                    return i % 2 == 0 ? Toward(live, Diamond.Rubber.X, Diamond.Rubber.Z, swap: select) : new LivePadInput(Swap: select);
                },
                observe: live =>
                {
                    if (live.Active && live.HoldsBall && !live.Throwing)
                        trace.Add((live.GlovePos, live.GloveX, live.GloveZ, live.BallX, live.BallZ));
                });
            return (run, trace, pressed);
        }

        var without = Play(select: false);
        var with = Play(select: true);
        Assert.Equal(presses, with.Presses);
        // The catcher's frames: the ball held behind the plate until the release (the receiver's frames at second follow).
        var catcher = with.Trace.TakeWhile(f => f.Pos == "C").ToList();
        Assert.True(catcher.Count >= presses, $"the catcher held the ball through the presses; held {catcher.Count} frames");

        // Nothing moves on a press: the ring on the catcher, the ball in their glove (the held ball is placed at the top of the
        // tick and the stick walks the glove after it, so it trails by one frame's step at most), the body within a walk of the plate.
        foreach (var (_, gx, gz, bx, bz) in catcher)
        {
            Assert.True(Diamond.Dist(gx, gz, bx, bz) < 1, $"the ball is in the catcher's glove; glove ({gx:0.0}, {gz:0.0}), ball ({bx:0.0}, {bz:0.0})");
            Assert.True(Diamond.Dist(bx, bz, catcherSpot.X, catcherSpot.Z) < 6, $"the ball stayed at the plate; got ({bx:0.0}, {bz:0.0})");
        }
        // A dead-stick press leaves the glove exactly where the last frame left it.
        for (var i = 1; i < presses; i += 2)
            Assert.Equal((catcher[i - 1].GX, catcher[i - 1].GZ), (catcher[i].GX, catcher[i].GZ));

        // Select the only difference: frame for frame the same glove and ball, the same throw, the same play.
        Assert.Equal(without.Trace, with.Trace);
        var throwWith = Assert.Single(with.Run.Throws);
        var throwWithout = Assert.Single(without.Run.Throws);
        Assert.Equal(2, throwWith.Bag);
        Assert.Equal("C", throwWith.FromPos);
        Assert.Equal((throwWithout.Bag, throwWithout.FromPos, throwWithout.ReleaseSec), (throwWith.Bag, throwWith.FromPos, throwWith.ReleaseSec));
        Assert.True(throwWith.ReleaseSec >= presses * Frame - 1e-9, "the throw came after the presses");
        Assert.Equal(without.Run.Play.Kind, with.Run.Play.Kind);
        Assert.Equal(PlayStamp.Label(without.Run.Play), PlayStamp.Label(with.Run.Play));
        var factsWithout = without.Run.Play.Outcome!;
        var factsWith = with.Run.Play.Outcome!;
        Assert.Equal(
            factsWithout.OutsMade.Select(o => (o.Type, o.Bag, o.FromBag, o.Runner.Id)),
            factsWith.OutsMade.Select(o => (o.Type, o.Bag, o.FromBag, o.Runner.Id)));
        Assert.Equal(
            factsWithout.Moves.Select(m => (m.Runner.Id, m.FromBag, m.ToBag)),
            factsWith.Moves.Select(m => (m.Runner.Id, m.FromBag, m.ToBag)));
        Assert.Equal(ThrowOrigin.Catcher, factsWith.ThrowEndpoint?.Origin);
    }

    [Fact]
    public void S99_OnAPickoffThatSailsSelectIsDeadInFlightThenTakesTheBodyTheStickNamesAndHoldsItForTheLock()
    {
        // The verb is not dead on a runner play: while the throw flies Select does nothing (§8.9, the ring rides the
        // receiver); once the ball is loose the press takes the body the stick names, the lock holds that ring for
        // chase.swapLockSec (a dead stick stands them still and the nearest-body hand-off waits), the ball rolls on by
        // itself, and the play still resolves as S-71.
        var lock_ = _content.Rules.Fielding.Chase.SwapLockSec;
        RunResult? sailed = null;
        var inFlightRing = new List<string>();
        var before = "";
        var expected = "";
        var pressed = false;
        var lockOnPress = double.NaN;
        var underLock = new List<(string Pos, double GX, double GZ, double BX, double BZ)>();
        var looseFrames = 0;
        for (var seed = 1; seed <= 60 && sailed is null; seed++)
        {
            inFlightRing.Clear();
            underLock.Clear();
            before = expected = "";
            pressed = false;
            lockOnPress = double.NaN;
            looseFrames = 0;
            // Ashlord at first: bad chemistry with the pitcher, the slant is the pair's (§8.5, S-71).
            // The C80 copy (#722): a bad pair is slow, never slanted, and Vale's own spread (Field 8, sigma 1.05 ft) never misses
            // the 6 ft cover: 0 pickoffs of 400 sail there. The sail on the copy is a wild arm's, so the row's pitcher has an
            // authored Arm of 1 (sigma 3.5 ft).
            var match = Defense(seed: seed, first: "ashlord", pitcherArm: 1);
            Station(match, [1]);
            Assert.True(match.StartSteal());
            var run = RunPickoff(match, 1, HumanCatcher, LivePlayCommandSource.Human,
                fieldPad: (i, live) =>
                {
                    var atPitcher = Toward(live, Diamond.Rubber.X, Diamond.Rubber.Z, swap: true);
                    if (live.ThrowInFlight)
                    {
                        inFlightRing.Add(live.GlovePos);
                        return atPitcher;
                    }
                    if (live.LooseBall && !pressed)
                    {
                        // The first loose frame: the ring is the nearest body's; Select points at another one.
                        pressed = true;
                        before = live.GlovePos;
                        expected = FieldAssist.NearestInDirection(before, live.Fielders, atPitcher.StickX, atPitcher.StickY);
                        return atPitcher;
                    }
                    return LivePadInput.Dead;
                },
                observe: live =>
                {
                    if (!live.Active || !live.LooseBall) return;
                    looseFrames++;
                    if (!pressed) return;
                    if (double.IsNaN(lockOnPress)) lockOnPress = live.SwapLock;
                    if (live.SwapLock > 0) underLock.Add((live.GlovePos, live.GloveX, live.GloveZ, live.BallX, live.BallZ));
                });
            if (run.Sailed) sailed = run;
        }
        Assert.NotNull(sailed);
        Assert.NotEmpty(inFlightRing);
        Assert.All(inFlightRing, pos => Assert.Equal("1B", pos)); // the ring rides the receiver; Select is dead in flight
        Assert.True(pressed, "the ball came loose after the flight");
        Assert.NotEqual(before, expected);

        // The press takes the body the stick names and arms the one lock.
        Assert.Equal(lock_, lockOnPress, 3);
        Assert.NotEmpty(underLock);
        Assert.All(underLock, f => Assert.Equal(expected, f.Pos));
        Assert.True(underLock.Count >= (int)Math.Round(lock_ / Frame) - 2, $"the lock held the ring {underLock.Count} frames");
        // Under the lock with a dead stick the body stands still, and the ball is loose, not in its glove.
        var at = (underLock[0].GX, underLock[0].GZ);
        // The response law (#718) ends the body's brake inside the lock: the last of it is one unit in the last place
        // of the double (3e-18 ft), so "still" there is within a billionth of a foot, not bit-equal.
        Assert.All(underLock, f => Assert.True(Diamond.Dist(at.GX, at.GZ, f.GX, f.GZ) < 1e-9, "the body stands still"));
        Assert.All(underLock, f => Assert.True(Diamond.Dist(f.GX, f.GZ, f.BX, f.BZ) > 1, "the ball is loose, not in the glove"));
        Assert.True(looseFrames > underLock.Count, "the chase went on after the lock lifted");
        // Still S-71: the sail is the ERROR and the runner who broke takes second.
        var facts = sailed!.Play.Outcome!;
        Assert.Contains(facts.Moves, m => m.FromBag == 1 && m.ToBag >= 2);
        Assert.Equal(PlayKind.StolenBase, sailed.Play.Kind);
        Assert.True(facts.Error, "the sail is the ERROR, not the steal");
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
        Assert.DoesNotContain(run.Throws, t => t.Bag == 4 && t.FromPos == "C");
        Assert.Equal(0, run.Play.RunsScored);

        // A Run-9 body armed inside the window, on a changeup, has the race: whichever way it goes it is the plate that decides.
        var burner = Defense(leadoff: "zig");
        Assert.True(burner.StationRunner(3, _content.Must("zig")));
        Assert.True(burner.StartSteal(windupSec: 0.2));
        burner.PitchSetup.Advance(.25);
        var slow = Scenario.PitchAt(0, StrikeZoneGeometry.Reference.CenterY, family: PitchFamily.Changeup);
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
        var rundown = false;
        var run = RunPickoff(match, 1, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu,
            runPad: (_, live) => { if (live.InRundown) rundown = true; ThrowsGoToTheBodyOnTheBag(match, live); return LivePadInput.Dead; });
        Assert.True(run.Broke, "the SET arm broke on the pitcher's first motion (D3)");
        Assert.Equal(1, run.Throws[0].Bag);
        Assert.Equal("P", run.Throws[0].FromPos);
        var facts = run.Play.Outcome!;
        // §4.5 / §11.4: the body is between bags with no head start; the receiver throws ahead or chases, and a tag at
        // first or second, or a rundown, decides it by geometry. A throw that sails is S-71's ERROR. A clean throw never
        // hangs at the bag while the runner walks in.
        var tagged = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        Assert.True(tagged is not null || run.Sailed, $"a runner between bags on a pickoff is tagged at first or second, or the throw sails; rundown seen: {rundown}");
        if (tagged is not null)
        {
            Assert.Equal(OutType.Tag, tagged.Type);
            Assert.Contains(tagged.Bag, new[] { 1, 2 });
            Assert.Equal(RunnerPlayResult.PickedOff, facts.RunnerResult);
            Assert.Equal("PICKED OFF", PlayStamp.Label(run.Play));
        }
        Assert.Equal(new ThrowEndpoint(ThrowOrigin.PitcherRubber, 1), facts.ThrowEndpoint);
    }

    [Theory]
    [InlineData("konga", 2)]
    [InlineData("fenn", 3)]
    [InlineData("brondo", 4)]
    public void S69_ThePickoffOnASlowRunnerWhoBrokeIsARundownTheThrowAheadEnds(string who, int run)
    {
        // A body slower than the glove (Run 2 to 4) stays inside the rundown range the whole way, so the receiver's read
        // is the rundown's (§9.7): the chase keeps them trapped with the ball behind them, the throw ahead goes at the
        // last makeable moment to the body the formation stood on second, and a body the ball beats both ways takes the
        // tag there (#640). Nothing about the runner, the glove, or the lob is tuned.
        var runner = _content.Must(who);
        Assert.Equal(run, runner.Stats.Run);
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", who, "cinder", "dart", "jester", "zig", "grit", "soot", "boom", "nugget");
        var match = Match.Exhibition(_content, home, away, 3, 1);
        Assert.True(match.StationRunner(1, runner));
        Assert.True(match.StartSteal());
        var rundown = false;
        var throwsToSecond = 0;
        var result = RunPickoff(match, 1, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu,
            runPad: (_, live) =>
            {
                if (live.InRundown) rundown = true;
                if (live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 2)
                {
                    throwsToSecond++;
                    Assert.Equal("1B", live.ThrowFromPos);
                    Assert.True(live.ArmedThrow?.SpeedMul >= 1 - 1e-9, "a throw that races a body to the bag is never the lazy lob");
                }
                ThrowsGoToTheBodyOnTheBag(match, live);
                return LivePadInput.Dead;
            });
        Assert.True(result.Broke);
        Assert.True(rundown, "a body the glove cannot gain on is chased inside running.rundown.rangeFt");
        Assert.Equal(1, throwsToSecond);
        var facts = result.Play.Outcome!;
        var tagged = Assert.Single(facts.OutsMade, o => o.Runner.Id == runner.Id);
        Assert.Equal((OutType.Tag, 2), (tagged.Type, tagged.Bag));
        Assert.Equal(RunnerPlayResult.PickedOff, facts.RunnerResult);
        Assert.Equal("PICKED OFF", PlayStamp.Label(result.Play));
    }

    /// <summary>
    /// §8.7 on a runner play (#640): every throw to a bag is addressed to the body the formation stood there — the one
    /// cover read of the play — so the receiver is on the bag (inside fielding.cover.radiusFt) the frame the throw pops.
    /// </summary>
    void ThrowsGoToTheBodyOnTheBag(Match match, LivePlaySystem live)
    {
        if (!live.Events.Contains(LiveEvent.ThrowPop) || live.ThrowBag is < 1 or > 4) return;
        var bag = Diamond.Bag(live.ThrowBag);
        Assert.True(live.Fielders.TryGetValue(live.CoverPos, out var at), $"the throw to bag {live.ThrowBag} names a receiver");
        Assert.True(Diamond.Dist(at.X, at.Z, bag.X, bag.Z) <= match.Rules.Fielding.Cover.RadiusFt,
            $"the throw to bag {live.ThrowBag} is addressed to {live.CoverPos} standing on it, not a body at ({at.X:0},{at.Z:0})");
    }

    [Fact]
    public void S69b_AnEarlyDepartureCanReturnBeforeAPickoff()
    {
        var match = Defense();
        Station(match, [1]);
        Assert.True(match.StartSteal(windupSec: .1));
        match.PitchSetup.Advance(.1);
        match.ReturnToBag();
        match.PitchSetup.Advance(.2);
        Assert.True(match.RunnerAt(1)!.IsOn(1));
        var ev = match.Pickoff(1);
        Assert.Equal(PlayKind.Pickoff, ev!.Kind);
        Assert.Empty(ev.Outcome!.OutsMade);
    }

    [Theory]
    // The CPU catcher's release carries a noise draw (§11); seed 2 is a close race it throws on rather than concedes.
    [InlineData("soot", false, 2)]
    [InlineData("lace", true, 1)]
    public void S70_AnEarlierPhysicalDepartureAgainstTheCatcherIsTheRace(string catcherId, bool niceRelease, int seed)
    {
        // Zig (Run 9) armed 0.2 s into the windup breaks 0.4 s early (D2). Against a Field-5 CPU catcher the body
        // is in ahead of the ball; against the roster's best arm (Field 8; the spec row names 9) released at once
        // (the human's Nice release) the tag is there.
        var match = Defense(catcher: catcherId, seed: seed);
        Assert.True(match.StationRunner(1, _content.Must("zig")));
        Assert.True(match.StartSteal(windupSec: 0.2));
        Assert.True(match.RunnerAt(1)!.Broke);
        match.PitchSetup.Advance(.25);
        var seats = niceRelease ? HumanCatcher : LiveSeats.CpuOnly;
        var source = niceRelease ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var script = new DefenseScript([2]);
        var run = RunSteal(match, Scenario.Paint, Scenario.Take, seats, source,
            fieldPad: niceRelease ? (i, live) => script.Next(live, match) : null);
        var facts = run.Play.Outcome!;
        if (niceRelease)
        {
            var throwToSecond = Assert.Single(run.Throws, t => t.Bag == 2);
            Assert.True(throwToSecond.ReleaseSec <= match.Rules.Fielding.Throw.ReleaseSec + Frame + 1e-9, $"released at once; got {throwToSecond.ReleaseSec:0.000}");
            var tag = Assert.Single(facts.OutsMade);
            Assert.Equal((OutType.Tag, 2, 1), (tag.Type, tag.Bag, tag.FromBag));
            Assert.Equal(PlayKind.CaughtStealing, run.Play.Kind);
        }
        else
        {
            Assert.Single(run.Throws, t => t.Bag == 2); // the catcher throws on a close race rather than conceding (§8.8)
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
            // The C80 copy (#722): a bad pair never slants and Vale's own spread never misses the cover, so no pickoff sails
            // there (0 of 400); the throw that sailed in this loop on the copy was first base's throw on to second. The row is the
            // pickoff's sail, so the copy's pitcher has an authored Arm of 1 and the sail must be the pickoff's own.
            var match = Defense(seed: seed, first: "ashlord", pitcherArm: 1);
            Station(match, [1]);
            Assert.True(match.StartSteal());
            var run = RunPickoff(match, 1, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
            if (run.Sailed && run.Throws.Count == 1) sailed = run;
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
    [Trait("Kind", "Balance")]
    public void CpuStealTableIsBaseByRunTimesSituationAndNeverIntoABody()
    {
        var cpu = _content.Rules.Running.Cpu;
        Assert.Equal(0, RunnerAi.StealBase(4, cpu));
        // Each anchor is the chance at its own Run, and the table holds past the last.
        foreach (var anchor in cpu.StealAnchors)
            Assert.Equal(anchor.Chance, RunnerAi.StealBase(anchor.Run, cpu), 6);
        Assert.Equal(cpu.StealAnchors[^1].Chance, RunnerAi.StealBase(cpu.StealAnchors[^1].Run + 3, cpu), 6);
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
        var armed = once.CpuBatter.ArmSteal();
        Assert.False(once.CpuBatter.ArmSteal());
        _ = armed;
    }

    [Fact]
    [Trait("Kind", "Balance")]
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
                if (match.CpuPitcher.PickoffBag() > 0) n++;
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
        match.PitchSetup.ReleaseBall();
        match.PitchSetup.Advance(PitchFlight.AirSeconds(match.PitchSpeedMph(match.PreparePitch(pitch)), match.Rules));
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
        Func<int, LivePlaySystem, LivePadInput>? fieldPad = null, Func<int, LivePlaySystem, LivePadInput>? runPad = null,
        Action<LivePlaySystem>? observe = null)
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
        return Drive(match, result, seats, seat, fieldPad, runPad, observe);
    }

    static RunResult Drive(Match match, RunResult result, LiveSeats seats, LivePlayCommandSource seat,
        Func<int, LivePlaySystem, LivePadInput>? fieldPad, Func<int, LivePlaySystem, LivePadInput>? runPad, Action<LivePlaySystem>? observe)
    {
        var live = match.LivePlay;
        ThrowRecord? inFlight = null;
        PlayEvent? play = null;
        // Throws the play began with (the pickoff) pop before the first tick.
        if (live.ThrowInFlight)
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

    /// <summary>A full stick from the glove toward (x, z), with Select pressed or not.</summary>
    static LivePadInput Toward(LivePlaySystem live, double x, double z, bool swap = false)
    {
        var dx = x - live.GloveX;
        var dz = z - live.GloveZ;
        var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
        return new LivePadInput(StickX: dx / len, StickY: dz / len, Swap: swap);
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
    Match Defense(string leadoff = "cinder", string catcher = "pewter", int seed = 1, string first = "lace", int pitcherArm = 0)
    {
        var shortstop = first == "ashlord" ? "lace" : "ashlord";
        var home = _content.Team("Defense", "vale", catcher, first, "frost", "basil", shortstop, "vine", "moss", "hex");
        if (pitcherArm > 0)
        {
            // An authored Arm on the mound (the throw's lateral spread is the thrower's Arm, §8.5).
            var wild = home.Captain with { Stats = home.Captain.Stats with { Arm = pitcherArm } };
            home = home with { Captain = wild, Roster = home.Roster.Select(c => c.Id == wild.Id ? wild : c).ToList() };
        }
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
