using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B rows owned by P5 (#567): outs, double plays, close plays, rundowns (§9.6, §9.7,
/// §10). S-40 … S-50 run on the CPU seat and on the human seat; S-51 … S-55, S-73 … S-76 and the
/// triple play follow. Every assertion names the out's type, bag, and runner from the typed
/// <see cref="PlayOutcome"/>, and a throw is an out only when the body it was for was still
/// short of the bag as the ball landed: two outs only if both arrivals win, otherwise one out
/// and the fielder's choice.
/// </summary>
public sealed class OutsScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static readonly LiveSeats HumanRunners = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
    static readonly LiveSeats BothHuman = new(HumanBats: true, HumanPitches: true, PlayerMustField: true, Versus: true);

    // ---------------------------------------------------------------------------------
    // S-40 … S-50  The double-play matrix (§10.4)
    // ---------------------------------------------------------------------------------

    /// <summary>One row of §10.4: the ball, the bodies on base, the outs at contact, and the human's throws (0 = step on the force bag).</summary>
    public sealed record DpRow(string Id, double Carry, double Launch, double Spray, int[] Runners, int Outs, int[] HumanPlays, string Fielder, string Leadoff = "cinder")
    {
        public override string ToString() => Id;
    }

    public static IEnumerable<object[]> DpRows()
    {
        yield return [new DpRow("S-40 6-4-3", 118, 4, -18, [1], 0, [2, 1], "SS")];
        yield return [new DpRow("S-41 4-6-3 / unassisted", 120, 4, 5, [1], 0, [0, 1], "2B")];
        yield return [new DpRow("S-42 5-4-3", 100, 4, -40, [1], 0, [2, 1], "3B")];
        yield return [new DpRow("S-43 3 then the tag", 92, 4, 41, [1], 0, [0, 2], "1B")];
        yield return [new DpRow("S-44 3-6-3", 110, 4, 38, [1], 0, [2, 1], "1B")];
        yield return [new DpRow("S-45 1-6-3", 62, 3, 1, [1], 0, [2, 1], "P")];
        yield return [new DpRow("S-46 5 unassisted then 3", 92, 4, -44, [1, 2], 0, [0, 1], "3B")];
        yield return [new DpRow("S-47 1-2-3", 38, 3, -6, [1, 2, 3], 0, [4, 1], "P")];
        yield return [new DpRow("S-48 3 then the tag at the plate", 92, 4, 41, [1, 2, 3], 0, [0, 4], "1B")];
        yield return [new DpRow("S-50 two outs", 118, 4, -18, [1], 2, [2], "SS")];
    }

    [Theory]
    [MemberData(nameof(DpRows))]
    public void DoublePlayMatrix_CpuSeat(DpRow row)
    {
        var match = Defense(row.Leadoff);
        Station(match, row.Runners);
        Assert.True(match.SetOuts(row.Outs));
        var hit = FlightFixtures.Landing(match.Park, row.Carry, row.Launch, row.Spray);
        var preview = match.PreviewHit(hit);
        Assert.Equal(row.Fielder, preview.Position);
        var run = Run(match, hit, preview, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        AssertDoublePlayRow(row, match, run);
    }

    [Theory]
    [MemberData(nameof(DpRows))]
    public void DoublePlayMatrix_HumanSeat(DpRow row)
    {
        var match = Defense(row.Leadoff);
        Station(match, row.Runners);
        Assert.True(match.SetOuts(row.Outs));
        var hit = FlightFixtures.Landing(match.Park, row.Carry, row.Launch, row.Spray);
        var preview = match.PreviewHit(hit);
        Assert.Equal(row.Fielder, preview.Position);
        // One press is one throw (§10.4): the human throws both legs, or walks onto the force bag.
        var script = new DefenseScript(row.HumanPlays);
        var run = Run(match, hit, preview, HumanGlove, LivePlayCommandSource.Human, fieldPad: (i, live) => script.Next(live, match));
        AssertDoublePlayRow(row, match, run);
    }

    void AssertDoublePlayRow(DpRow row, Match match, RunResult run)
    {
        var play = run.Play;
        var facts = play.Outcome!;
        var outs = facts.OutsMade;
        Assert.NotEmpty(outs);
        Assert.True(outs.Count <= 3);
        // Every out is typed by where and how it was made (§10.1).
        foreach (var o in outs)
        {
            Assert.NotNull(o.Fielder);
            Assert.NotNull(o.Runner);
            if (o.FromBag == 0 && o.Bag == 1) Assert.Equal(OutType.ThrowOutAtFirst, o.Type);
            if (o.Type == OutType.Force) Assert.Equal(o.FromBag + 1, o.Bag);
        }
        // The force at second (and everywhere) is removed the moment the batter is retired first: later outs are tags (S-43, S-48).
        var batterOutIndex = outs.ToList().FindIndex(o => o.FromBag == 0);
        if (batterOutIndex >= 0)
            foreach (var later in outs.Skip(batterOutIndex + 1))
                Assert.Equal(OutType.Tag, later.Type);
        // A throw is an out only when the body it was for was still short of the bag as the ball landed (§10.2).
        foreach (var t in run.Throws.Where(t => t.Landed))
        {
            var made = outs.FirstOrDefault(o => o.Bag == t.Bag && o.Runner.Id == t.TargetId);
            if (made is not null && made.Type != OutType.Tag)
                Assert.True(t.TargetArrivalBeforeLanding > 0, $"{row.Id}: the out at {t.Bag} on {t.TargetId} needs the body short of the bag; arrival {t.TargetArrivalBeforeLanding:0.00}");
        }
        if (row.Outs == 2)
        {
            // No double-play attempt with two outs (S-50): the first makeable out ends the inning.
            Assert.Single(outs);
            Assert.Equal(0, play.OutsAfter);
            Assert.NotEqual(play.Context.Top, play.NextState.Top);
            return;
        }
        if (outs.Count >= 2)
        {
            Assert.Equal(outs.Count >= 3 ? "TRIPLE PLAY" : "DOUBLE PLAY", PlayStamp.Label(play));
            Assert.False(facts.FieldersChoice);
        }
        else
        {
            // One out and the batter on first: the fielder's choice (§10.4). The stamp and the caption both name it, from the typed flag.
            Assert.Equal(1, facts.BatterToBag);
            Assert.True(facts.FieldersChoice, $"{row.Id}: one out with the batter at first is the fielder's choice");
            Assert.Contains("Fielder's choice", play.Caption);
            Assert.Equal(PlayStamp.FieldersChoice, PlayStamp.Label(play));
        }
        // Row-specific shapes.
        switch (row.Id[..4])
        {
            case "S-40" or "S-42" or "S-44" or "S-45":
                Assert.Equal(2, run.Throws.First().Bag);
                Assert.Equal((OutType.Force, 2, 1), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                if (outs.Count == 2) Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (outs[1].Type, outs[1].Bag, outs[1].FromBag));
                break;
            case "S-41":
                // Within unassistedFt of the bag the fielder steps on it (S-41); the force is the glove's, not a throw's.
                Assert.Equal((OutType.Force, 2, 1), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                if (run.GloveToBagAtPossession <= match.Rules.Fielding.Throw.UnassistedFt)
                    Assert.DoesNotContain(run.Throws, t => t.Bag == 2);
                break;
            case "S-43":
                Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                Assert.DoesNotContain(run.Throws, t => t.Bag == 1);
                if (outs.Count == 2) Assert.Equal((OutType.Tag, 1), (outs[1].Type, outs[1].FromBag));
                break;
            case "S-46":
                Assert.Equal((OutType.Force, 3, 2), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                Assert.DoesNotContain(run.Throws, t => t.Bag == 3);
                if (outs.Count >= 2) Assert.Contains(outs[1].Bag, new[] { 1, 2 });
                break;
            case "S-47":
                Assert.Equal(4, run.Throws.First().Bag);
                Assert.Equal((OutType.Force, 4, 3), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                break;
            case "S-48":
                Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
                Assert.DoesNotContain(run.Throws, t => t.Bag == 1);
                Assert.DoesNotContain(outs, o => o.FromBag == 3 && o.Type != OutType.Tag);
                break;
        }
    }

    [Fact]
    public void S49_BuntPoppedUpWithTheRunnerSentIsCaughtThenDoubledOffFirst()
    {
        // The runner on first goes at contact (LB); the pop is caught in front of the plate and the
        // throw back to first is a force back (§10.5): out if the ball beats the body to the bag.
        var match = Defense("cinder");
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Hit(match.Park, 34, 58, 2) with { Foul = false, InPlay = true };
        var preview = match.PreviewHit(hit);
        Assert.False(preview.Grounder);
        Assert.False(FieldingResolver.IsOutfield(preview.Position), preview.Position);
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, _) => i < 3 ? new LivePadInput(AllAdvance: true) : LivePadInput.Dead);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal(OutType.Catch, outs[0].Type);
        Assert.Equal(0, outs[0].FromBag);
        var back = run.Throws.FirstOrDefault(t => t.Bag == 1 && t.Landed);
        var doubled = outs.FirstOrDefault(o => o.FromBag == 1);
        if (doubled is not null)
        {
            // Off the bag at the catch: the force back at first (§10.5). On the bag with the send: the tag-up, and the
            // tag at second is the infielder's play on a body going (§8.8).
            if (doubled.Type == OutType.Force) Assert.Equal((1, runner.Id), (doubled.Bag, doubled.Runner.Id));
            else Assert.Equal((OutType.Tag, 2, runner.Id), (doubled.Type, doubled.Bag, doubled.Runner.Id));
            Assert.Equal("DOUBLE PLAY", PlayStamp.Label(run.Play));
        }
        else
            Assert.Equal(1, match.Runners.Count(r => r.Who.Id == runner.Id));
        _ = back;
    }

    // ---------------------------------------------------------------------------------
    // S-51 … S-55  Doubled off and the tag-up (§10.5)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S51_RunnerSentOnContactIsDoubledOffFirstWhenTheCatchAndTheThrowBeatThemBack()
    {
        var match = Defense("cinder");
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Landing(match.Park, 112, 58, -14);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        // On a ball in the air LB is tag-and-go (§9.5); the send that overrides the hold is the stick on the selected runner.
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, _) => i < 3 ? new LivePadInput(KeysBag: 1, StickBag: 2) : LivePadInput.Dead);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal((OutType.Catch, 0), (outs[0].Type, outs[0].FromBag));
        Assert.True(run.LeftEarlySeen, "the body was off the bag at the catch and owed the retouch (§9.5)");
        var doubled = outs.FirstOrDefault(o => o.Runner.Id == runner.Id);
        var back = run.Throws.FirstOrDefault(t => t.Bag == 1 && t.Landed);
        if (doubled is not null)
        {
            Assert.Equal((OutType.Force, 1, 1), (doubled.Type, doubled.Bag, doubled.FromBag));
            Assert.NotNull(back);
            Assert.True(back!.TargetArrivalBeforeLanding > 0, "doubled off only when the ball beat the body back");
            Assert.Equal("DOUBLE PLAY", PlayStamp.Label(run.Play));
        }
        else
            Assert.NotNull(match.First);
    }

    [Fact]
    public void S52_RunnerFromSecondSentOnAFlyToCenterIsDoubledOffByTheThrowBackToSecond()
    {
        var match = Defense("cinder");
        Station(match, [2]);
        var runner = match.Second!;
        var hit = FlightFixtures.Landing(match.Park, 280, 30, 2);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, _) => i < 3 ? new LivePadInput(KeysBag: 2, StickBag: 3) : LivePadInput.Dead);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal((OutType.Catch, 0), (outs[0].Type, outs[0].FromBag));
        Assert.True(run.LeftEarlySeen);
        Assert.Contains(run.Throws, t => t.Bag == 2 || t.Bag == 0);
        var doubled = outs.FirstOrDefault(o => o.Runner.Id == runner.Id);
        if (doubled is not null)
        {
            Assert.Equal((OutType.Force, 2, 2), (doubled.Type, doubled.Bag, doubled.FromBag));
            Assert.Equal("DOUBLE PLAY", PlayStamp.Label(run.Play));
        }
        else
            Assert.NotNull(match.Second);
    }

    [Fact]
    public void S53_RunnerOnThirdHoldingOnACaughtBallIsOneOutAndStays()
    {
        var match = Defense("cinder");
        Station(match, [3]);
        var runner = match.Third!;
        var hit = FlightFixtures.Landing(match.Park, 112, 58, -14);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var run = Run(match, hit, preview, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        var only = Assert.Single(run.Play.Outcome!.OutsMade);
        Assert.Equal((OutType.Catch, 0), (only.Type, only.FromBag));
        Assert.Equal(runner.Id, match.Third?.Id);
        Assert.Equal(0, run.Play.RunsScored);
    }

    [Theory]
    [InlineData("konga")]
    [InlineData("cinder")]
    [InlineData("dart")]
    public void S54_TagUpFromThirdOnADeepFlyIsARaceHomeAndTheIconOnlyInsideTheMargin(string who)
    {
        // The roster's best outfield arm is Field 8 (vine in left); the spec row names 9. The relay comes
        // through the cutoff (§8.7); a run at stake is thrown for when the ball can land inside the margin.
        var match = Defense("boom");
        var runner = _content.Must(who);
        Assert.NotEqual(runner.Id, match.Batter.Id);
        Assert.True(match.StationRunner(3, runner));
        var hit = FlightFixtures.Landing(match.Park, 222, 34, -30);
        var preview = match.PreviewHit(hit);
        Assert.Equal("LF", preview.Position);
        var run = Run(match, hit, preview, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        var facts = run.Play.Outcome!;
        Assert.Equal(OutType.Catch, facts.OutsMade[0].Type);
        Assert.True(run.TaggedUp, "the runner on third tags on a fly this deep with fewer than two outs (§9.5)");
        var tagged = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        var scored = facts.Moves.Any(m => m.Runner.Id == runner.Id && m.ToBag == 4);
        Assert.True((tagged is not null) ^ scored, "out at the plate or the run: one of them");
        if (tagged is not null) Assert.Equal((OutType.Tag, 4, 3), (tagged.Type, tagged.Bag, tagged.FromBag));
        var home = run.Throws.FirstOrDefault(t => t.Bag == 4 && t.Landed);
        if (home is null)
        {
            // Nobody could play at the plate: safe by arrival, no contest.
            Assert.True(scored);
            Assert.False(run.SawIcon);
            return;
        }
        // The close-play icon only when the ball was there ahead of the body by no more than the margin (§9.6, D5).
        var margin = match.Rules.Running.Close.MarginSec;
        var within = home.TargetArrivalBeforeLanding > 0 && home.TargetArrivalBeforeLanding <= margin + Frame;
        Assert.Equal(within, run.SawIcon);
        if (home.TargetArrivalBeforeLanding > margin + Frame) Assert.NotNull(tagged);
    }

    [Fact]
    public void S55_PopDroppedOnPurposeWithTheBasesLoadedIsLiveWithTheForceAtHomeOnly()
    {
        var match = Defense("cinder");
        Station(match, [1, 2, 3]);
        var third = match.Third!;
        var hit = FlightFixtures.Landing(match.Park, 120, 62, -12);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var homeForced = false;
        var dropSeen = false;
        var script = new DefenseScript([4]);
        var run = Run(match, hit, preview, HumanGlove, LivePlayCommandSource.Human,
            fieldPad: (i, live) =>
            {
                // Step off the landing until the ball is on the ground, pick it up, then the throw home.
                if (live.Fly == FlyState.InAir && !live.HoldsBall) return new LivePadInput(StickX: 1);
                if (!live.HoldsBall)
                {
                    var dx = live.BallX - live.GloveX;
                    var dz = live.BallZ - live.GloveZ;
                    var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                    return new LivePadInput(StickX: dx / len, StickY: dz / len, SouthDown: true);
                }
                return script.Next(live, match);
            },
            observe: live =>
            {
                if (live.Fly == FlyState.Dropped) dropSeen = true;
                if (live.Fly == FlyState.Dropped && live.Forces.At(4)) homeForced = true;
            });
        Assert.True(dropSeen, "the pop was let drop (no infield fly rule, D9)");
        Assert.True(homeForced, "at the drop the bases-loaded force at home stands");
        var facts = run.Play.Outcome!;
        Assert.DoesNotContain(facts.OutsMade, o => o.Type == OutType.Catch);
        var atHome = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == third.Id);
        if (atHome is not null) Assert.Equal((OutType.Force, 4, 3), (atHome.Type, atHome.Bag, atHome.FromBag));
        else Assert.Contains(facts.Moves, m => m.Runner.Id == third.Id && m.ToBag == 4);
        Assert.DoesNotContain(facts.OutsMade, o => o.Type == OutType.Tag && o.Bag == 4);
    }

    // ---------------------------------------------------------------------------------
    // S-73 … S-75  The close play only inside the margin (§9.6, D5)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S73_ThrowWellAheadOfTheRunnerAtThirdIsATagWithNoIcon()
    {
        var (match, runner, hit, preview) = ThirdBaseRace(orderIndex: 2, spray: 30);
        Assert.Equal(4, runner.Stats.Run);
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, _) => i < 3 ? new LivePadInput(AllAdvance: true) : LivePadInput.Dead);
        var third = Assert.Single(run.Throws, t => t.Bag == 3 && t.Landed);
        Assert.True(third.TargetArrivalBeforeLanding > match.Rules.Running.Close.MarginSec + Frame,
            $"the body was {third.TargetArrivalBeforeLanding:0.00} s out when the ball landed");
        Assert.False(run.SawIcon, "outside the margin the geometry decides silently");
        var tag = Assert.Single(run.Play.Outcome!.OutsMade, o => o.Runner.Id == runner.Id);
        Assert.Equal((OutType.Tag, 3, 2), (tag.Type, tag.Bag, tag.FromBag));
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(45, false)]
    public void S74_ThrowJustAheadOfTheRunnerRunsTheMashAndTheFirstPressWins(int pressFramesAfterIcon, bool safe)
    {
        // The dash puts the body inside the margin; the offense's press after the icon races the CPU glove's reaction.
        var (match, runner, hit, preview) = ThirdBaseRace(orderIndex: 7, spray: 38);
        Assert.Equal(5, runner.Stats.Run);
        var iconAt = -1;
        var pressed = false;
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, live) =>
            {
                if (iconAt < 0 && live.CloseIcon) iconAt = i;
                if (iconAt >= 0 && !pressed && i >= iconAt + pressFramesAfterIcon)
                {
                    pressed = true;
                    return new LivePadInput(SouthDown: true);
                }
                if (i < 3) return new LivePadInput(AllAdvance: true);
                if (i is >= 3 and < 20 && i % 4 == 0 && !live.InClosePlay) return new LivePadInput(SouthDown: true); // the dash
                return LivePadInput.Dead;
            });
        var third = Assert.Single(run.Throws, t => t.Bag == 3 && t.Landed);
        Assert.True(third.TargetArrivalBeforeLanding > 0 && third.TargetArrivalBeforeLanding <= match.Rules.Running.Close.MarginSec + Frame,
            $"the body was {third.TargetArrivalBeforeLanding:0.00} s out when the ball landed");
        Assert.True(run.SawIcon, "inside the margin the icon comes up");
        var facts = run.Play.Outcome!;
        if (safe)
        {
            Assert.DoesNotContain(facts.OutsMade, o => o.Runner.Id == runner.Id);
            Assert.Contains(facts.Moves, m => m.Runner.Id == runner.Id && m.ToBag == 3);
            Assert.Contains(LiveEvent.StampSafe, run.EventsSeen);
        }
        else
        {
            var tag = Assert.Single(facts.OutsMade, o => o.Runner.Id == runner.Id);
            Assert.Equal((OutType.Tag, 3, 2), (tag.Type, tag.Bag, tag.FromBag));
        }
    }

    [Fact]
    public void S75_NobodyPressesAndTheCpuReactionDecides()
    {
        var (match, runner, hit, preview) = ThirdBaseRace(orderIndex: 7, spray: 38);
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, live) =>
            {
                if (i < 3) return new LivePadInput(AllAdvance: true);
                if (i is >= 3 and < 20 && i % 4 == 0 && !live.InClosePlay) return new LivePadInput(SouthDown: true);
                return LivePadInput.Dead;
            });
        Assert.True(run.SawIcon);
        // The human never presses; the CPU glove's reaction (running.close.cpuReaction*) is the only press, and it wins.
        var tag = Assert.Single(run.Play.Outcome!.OutsMade, o => o.Runner.Id == runner.Id);
        Assert.Equal((OutType.Tag, 3, 2), (tag.Type, tag.Bag, tag.FromBag));
        Assert.Contains("tags the runner at third", run.Play.Caption);
    }

    // ---------------------------------------------------------------------------------
    // S-76  The rundown (§9.7)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S76_RunnerOffFirstWithTheBallInTheGloveNearbyIsARundownEndedByTagBagOrOverthrow()
    {
        // Grounder to the first baseman on the bag: the batter is retired there, the force is gone, and the
        // human's runner, sent at contact and turned back once the batter is out, is caught between the bags.
        var match = Defense("cinder");
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Landing(match.Park, 92, 4, 41);
        var preview = match.PreviewHit(hit);
        Assert.Equal("1B", preview.Position);
        var rundownFrames = 0;
        var chased = false;
        var turnedBack = false;
        var sentAgain = false;
        var run = Run(match, hit, preview, HumanRunners, LivePlayCommandSource.Human,
            runPad: (i, live) =>
            {
                if (i < 3) return new LivePadInput(AllAdvance: true);
                var body = match.RunnerAt(1);
                if (body is not { Live: true }) return LivePadInput.Dead;
                // The batter is out: hold RB to bring the runner back (a tap halts, holding turns them, §9.3); then, ten
                // feet from the glove waiting on first, the stick sends them forward again (S-76).
                if (live.BatterOut && !turnedBack && body.DestBag > body.Bag && body.Feet < 40)
                    turnedBack = true;
                if (turnedBack && body.Phase != RunnerPhase.Returning && body.DestBag > body.Bag && !sentAgain)
                    return new LivePadInput(AllReturn: true);
                if (turnedBack && body.Phase == RunnerPhase.Returning && body.Feet < 12)
                {
                    sentAgain = true;
                    return new LivePadInput(KeysBag: 1, StickBag: 2);
                }
                return LivePadInput.Dead;
            },
            observe: live =>
            {
                if (live.InRundown)
                {
                    rundownFrames++;
                    if (live.RundownRunner?.Who.Id == runner.Id && live.HoldsBall) chased = true;
                }
            });
        var facts = run.Play.Outcome!;
        Assert.Contains(facts.OutsMade, o => o.FromBag == 0 && o.Bag == 1 && o.Type == OutType.ThrowOutAtFirst);
        Assert.True(rundownFrames > 0, "the body off the bag with the ball in reach is a rundown");
        Assert.True(chased);
        Assert.Contains(LiveEvent.Rundown, run.EventsSeen);
        // It ends by a tag, by the runner reaching a bag, or by a throw that got away (live).
        var tag = facts.OutsMade.FirstOrDefault(o => o.Runner.Id == runner.Id);
        var onABag = match.Runners.Any(r => r.Who.Id == runner.Id);
        if (tag is not null) Assert.Equal(OutType.Tag, tag.Type);
        else Assert.True(onABag || run.EventsSeen.Contains(LiveEvent.ThrowSailed), "safe on a bag, or the overthrow let them go");
    }

    [Fact]
    public void RundownCpuRunnerReversesOnEveryThrow()
    {
        // A CPU body caught between first and second runs away from the ball (§9.7): a throw to the bag ahead turns
        // them back, a throw to the bag behind sends them on again. Nothing else in the read changes.
        var who = _content.Must("cinder");
        var runner = new Runner(who, 1);
        runner.BeginPlay(forced: false, tagAndGo: false);
        runner.Send(2);
        // Past the commit fraction (running.cpu.commitFraction): the ordinary read keeps a body this far along going.
        RunnerSystem.Tick([runner], 1.5, new RunnerTickContext(1.5, 0, FlyState.None, 0, _ => false, _ => false));
        Assert.True(runner.Feet > 40 && runner.DestBag == 2);
        runner.MarkRundown(true);
        var second = Diamond.Bag(2);
        var first = Diamond.Bag(1);
        RunnerAiContext Throwing(int bag) => new(1.0, 0, int.MinValue, FlyState.None,
            new BallSituation(false, true, bag, 1.8, bag == 2 ? second.X : first.X, bag == 2 ? second.Z : first.Z, 1.0, false, 60, 70, 90), 0);
        RunnerAi.Decide([runner], Throwing(2), _ => false);
        Assert.Equal(1, runner.DestBag);
        RunnerAi.Decide([runner], Throwing(1), _ => false);
        Assert.Equal(2, runner.DestBag);
        // Off the rundown the same throw does not turn a committed body around.
        runner.MarkRundown(false);
        RunnerAi.Decide([runner], Throwing(2), _ => false);
        Assert.Equal(2, runner.DestBag);
    }

    // ---------------------------------------------------------------------------------
    // §10.7  The triple play through the rules
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TriplePlayIsReachableThroughTheForcesAloneAndStampsTriplePlay()
    {
        // Runners on first and second, a hard grounder to the third baseman beside the bag: step on
        // third, the force at second, the throw to first — three bodies short of their bags (§10.7).
        var match = Defense("cinder");
        Station(match, [1, 2]);
        var hit = FlightFixtures.Landing(match.Park, 92, 4, -44);
        var preview = match.PreviewHit(hit);
        Assert.Equal("3B", preview.Position);
        var run = Run(match, hit, preview, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal(3, outs.Count);
        Assert.Equal((OutType.Force, 3, 2), (outs[0].Type, outs[0].Bag, outs[0].FromBag));
        Assert.Equal((OutType.Force, 2, 1), (outs[1].Type, outs[1].Bag, outs[1].FromBag));
        Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (outs[2].Type, outs[2].Bag, outs[2].FromBag));
        foreach (var t in run.Throws.Where(t => t.Landed))
            Assert.True(t.TargetArrivalBeforeLanding > 0, $"the body was short at {t.Bag}");
        Assert.Equal("TRIPLE PLAY", PlayStamp.Label(run.Play));
        Assert.StartsWith("Triple play!", run.Play.Caption);
        Assert.Equal(0, run.Play.OutsAfter);
    }

    [Fact]
    public void AFailedRetireNarratesNothing()
    {
        // With two outs already and the third recorded on the play, a later throw to a bag cannot retire
        // anyone: the step reports no out, no force, no caption (§10.4, A.5 #49).
        var match = Defense("cinder");
        Station(match, [1]);
        Assert.True(match.SetOuts(2));
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        var run = Run(match, hit, preview, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        Assert.Single(run.Play.Outcome!.OutsMade);
        Assert.Equal(0, run.Play.OutsAfter);
        Assert.DoesNotContain("turns two", run.Play.Caption);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>One throw of the live ball: where it went and how far the body it was for still had to run as it landed.</summary>
    public sealed class ThrowRecord
    {
        public int Bag;
        public string TargetId = "";
        public double TargetArrivalBeforeLanding = double.NaN;
        public bool Landed;
    }

    public sealed class RunResult
    {
        public PlayEvent Play = null!;
        public List<ThrowRecord> Throws = new();
        public HashSet<LiveEvent> EventsSeen = new();
        public bool SawIcon;
        public bool RundownSeen;
        public bool LeftEarlySeen;
        public bool TaggedUp;
        public double GloveToBagAtPossession = double.NaN;
    }

    RunResult Run(Match match, AtBatResult hit, FieldingPreview preview, LiveSeats seats, LivePlayCommandSource seat,
        Func<int, LivePlaySystem, LivePadInput>? fieldPad = null, Func<int, LivePlaySystem, LivePadInput>? runPad = null,
        Action<LivePlaySystem>? observe = null)
    {
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats, 0, seat)).Snapshot.Active);
        var result = new RunResult();
        ThrowRecord? inFlight = null;
        var wasHolding = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var field = fieldPad?.Invoke(i, live) ?? LivePadInput.Dead;
            var run = runPad?.Invoke(i, live) ?? LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, field, run, false, seat));
            foreach (var e in live.Events) result.EventsSeen.Add(e);
            if (live.CloseIcon) result.SawIcon = true;
            if (live.InRundown) result.RundownSeen = true;
            if (live.Events.Contains(LiveEvent.ThrowPop))
            {
                inFlight = new ThrowRecord { Bag = live.ThrowBag };
                result.Throws.Add(inFlight);
            }
            if (inFlight is not null && live.Throwing)
            {
                var target = TargetOf(match, live, inFlight.Bag);
                // The body the throw was for is already home or on the bag: no race left (the ball is late).
                if (target is null && inFlight.TargetId != "") inFlight.TargetArrivalBeforeLanding = 0;
                if (target is not null)
                {
                    inFlight.TargetId = target.Who.Id;
                    // The race the throw is judged on: the way back for a body owing a retouch (§10.5), the way forward otherwise.
                    inFlight.TargetArrivalBeforeLanding = target.LeftEarly && target.FromBag == inFlight.Bag
                        ? RunnerSystem.ReturnSec(target, live.Dash01, match.Rules)
                        : RunnerSystem.ArrivalSec(target, inFlight.Bag, live.ElapsedSeconds, live.Dash01, match.Rules);
                }
            }
            if (inFlight is not null && !live.Throwing)
            {
                inFlight.Landed = !live.LooseBall;
                inFlight = null;
            }
            if (live.HoldsBall && !live.Throwing && !wasHolding && double.IsNaN(result.GloveToBagAtPossession))
            {
                var second = Diamond.Bag(2);
                result.GloveToBagAtPossession = Diamond.Dist(live.GloveX, live.GloveZ, second.X, second.Z);
            }
            wasHolding = live.HoldsBall && !live.Throwing;
            foreach (var body in match.Runners)
            {
                if (body.LeftEarly) result.LeftEarlySeen = true;
                if (!body.IsBatter && body.FromBag == 3 && live.Fly == FlyState.Caught && body.Live && body.Feet > 0) result.TaggedUp = true;
            }
            observe?.Invoke(live);
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        result.Play = play!;
        return result;
    }

    /// <summary>The body a throw to <paramref name="bag"/> is for: the forced runner behind it, else the one heading there, else one owing a retouch there.</summary>
    static Runner? TargetOf(Match match, LivePlaySystem live, int bag)
    {
        if (bag is < 1 or > 4) return null;
        if (live.Forces.At(bag))
        {
            var forced = match.RunnerAt(bag - 1);
            if (forced is { Live: true } && forced.Bag < bag) return forced;
        }
        var heading = RunnerSystem.HeadingTo(match.Runners, bag);
        if (heading is not null) return heading;
        return match.Runners.FirstOrDefault(r => r.Live && r.LeftEarly && r.FromBag == bag);
    }

    /// <summary>The human glove's script (§10.4): a throw to a bag per press, or a walk onto a force bag (0 = the next force bag ahead of the glove's play).</summary>
    sealed class DefenseScript
    {
        readonly int[] _plays;
        int _index;
        bool _pending;
        int _outsAtStep = -1;

        public DefenseScript(int[] plays) => _plays = plays;

        public LivePadInput Next(LivePlaySystem live, Match match)
        {
            if (_pending && live.Throwing) { _pending = false; _index++; _outsAtStep = -1; }
            if (_index >= _plays.Length) return LivePadInput.Dead;
            if (!live.HoldsBall || live.Throwing || live.LooseBall || live.InClosePlay) return LivePadInput.Dead;
            var play = _plays[_index];
            if (play > 0)
            {
                _pending = true;
                return new LivePadInput(KeysBag: play, SouthDown: true);
            }
            // Walk onto the nearest bag a force (or a retouch) stands at.
            if (_outsAtStep < 0) _outsAtStep = match.Outs;
            if (match.Outs > _outsAtStep) { _index++; _outsAtStep = -1; return LivePadInput.Dead; }
            var bag = ForceBagNear(live, match);
            if (bag == 0) return LivePadInput.Dead;
            var at = Diamond.Bag(bag);
            var dx = at.X - live.GloveX;
            var dz = at.Z - live.GloveZ;
            var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
            return new LivePadInput(StickX: dx / len, StickY: dz / len);
        }

        static int ForceBagNear(LivePlaySystem live, Match match)
        {
            var best = 0;
            var bestD = double.MaxValue;
            for (var bag = 1; bag <= 4; bag++)
            {
                var stands = live.Forces.At(bag) && match.RunnerAt(bag - 1) is { Live: true } r && r.Bag < bag
                             || match.Runners.Any(x => x.Live && x.LeftEarly && x.FromBag == bag);
                if (!stands) continue;
                var at = Diamond.Bag(bag);
                var d = Diamond.Dist(live.GloveX, live.GloveZ, at.X, at.Z);
                if (d < bestD) { bestD = d; best = bag; }
            }
            return best;
        }
    }

    /// <summary>Runner on second sent at contact on a sharp grounder to the right side; the CPU glove throws to third (S-37).</summary>
    (Match Match, Character Runner, AtBatResult Hit, FieldingPreview Preview) ThirdBaseRace(int orderIndex, double spray)
    {
        var scenario = new Scenario(_content, seed: 5).Runner(2, orderIndex);
        var match = scenario.Match;
        var runner = match.Second!;
        var hit = FlightFixtures.Hit(match.Park, 90, 3, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Position is "1B" or "2B", preview.Position);
        return (match, runner, hit, preview);
    }

    /// <summary>Home nine on defense: captain vale on the mound, lace at first, ashlord at short, the rest a neutral mix; the away leadoff bats.</summary>
    Match Defense(string leadoff, int seed = 1)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", leadoff, "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        return Match.Exhibition(_content, home, away, 3, seed);
    }

    /// <summary>Station the away order's next hitters on the given bags (bag 1 gets the second hitter, and so on).</summary>
    static void Station(Match match, int[] bags)
    {
        var roster = match.Away.Roster.ToList();
        foreach (var bag in bags)
            Assert.True(match.StationRunner(bag, roster[bag + 1]), $"station bag {bag}");
    }
}
