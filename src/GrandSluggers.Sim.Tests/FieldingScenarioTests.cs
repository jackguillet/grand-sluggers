using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B rows owned by P4 (#566): fielding decides by geometry (§7, §8). S-31 … S-35 and
/// the §7 scenes as scenarios. S-33 (the human dead stick scoops but never throws) lives in
/// <see cref="SeatOwnershipTests"/>. Nothing here rolls an out: a glove reaches the ball or it
/// does not, a throw lands inside its cover's reach or it does not, and the runner bodies race
/// the one throw clock.
/// </summary>
public sealed class FieldingScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly PitchCommand Paint = Scenario.Paint;
    static readonly SwingCommand Swing = Scenario.Swing;

    // ---------------------------------------------------------------------------------
    // S-31 / S-32  The routine out and the infield single are the same geometry with a different body
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S31_RoutineGrounderToShortWithARunFiveBatterIsAForceAtFirstByGeometry()
    {
        var match = Defense(leadoff: "cinder");
        Assert.Equal(5, match.Batter.Stats.Run);
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var batter = match.Batter;
        var (play, throwLandedAt, thrown) = RunCpu(match, hit, preview, out _);

        Assert.Equal(PlayKind.GroundOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (only.Type, only.Bag, only.FromBag));
        // The out is the arrival compare (§10.2): the ball was at first before the body could be.
        var body = Runner.BatterRunner(batter, HomeSet.BatterBodyX(batter.Bats), HomeSet.BatterZ);
        var batterAt = RunnerSystem.ArrivalSec(body, 1, 0, 0, match.Rules);
        Assert.True(throwLandedAt > 0 && throwLandedAt < batterAt, $"ball at first {throwLandedAt:0.00} vs body {batterAt:0.00}");
        Assert.NotNull(thrown);
        Assert.Equal(1.0, thrown!.SpeedMul / (InPlay.ArmMul(match.Defense.Roster.First(c => c.Id == "ashlord"), match.Rules)), 3);
    }

    [Fact]
    public void S32_SameGrounderWithARunNineBatterAndAWeakArmAtShortIsAnInfieldSingleNoRoll()
    {
        // The spec row names Run 10 and Field 2; the roster tops out at Run 9 (dart), and every Field-2
        // glove carries the Laser arm, so the weak arm here is ashlord (Field 3, no throw ability).
        var match = Defense(leadoff: "dart");
        Assert.Equal(9, match.Batter.Stats.Run);
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var (play, throwLandedAt, _) = RunCpu(match, hit, preview, out _);

        Assert.Equal(PlayKind.Single, play.Kind);
        Assert.Empty(play.Outcome!.OutsMade);
        Assert.Equal(1, play.Outcome.BatterToBag);
        // Either the fielder held (nothing makeable, §8.8 rule 5) or the throw landed after the body.
        if (throwLandedAt > 0)
        {
            var batter = match.Log[^1].Batter;
            var body = Runner.BatterRunner(batter, HomeSet.BatterBodyX(batter.Bats), HomeSet.BatterZ);
            Assert.True(throwLandedAt >= RunnerSystem.ArrivalSec(body, 1, 0, 0, match.Rules));
        }
    }

    // ---------------------------------------------------------------------------------
    // S-34  A human throw to a bag with nobody to play on is a wasted throw, named
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S34_HumanArmsThirdWithNobodyOnAndThrowsThere_BatterSafeAndTheCaptionNamesIt()
    {
        var match = Defense(leadoff: "cinder");
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        Assert.Equal("SS", preview.Position);
        var seats = new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, null, seats, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        PlayEvent? play = null;
        var threwTo = 0;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            // Dead stick: the CPU runs the glove and scoops (S-33). With the ball, arm third and press South.
            var pad = live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: 3, SouthDown: true) : LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, LivePlayCommandSource.Human));
            if (live.Throwing && threwTo == 0) threwTo = live.ThrowBag;
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.Equal(3, threwTo);
        Assert.Equal(PlayKind.Single, play!.Kind);
        Assert.Empty(play.Outcome!.OutsMade);
        Assert.Equal(1, play.Outcome.BatterToBag);
        Assert.Contains("throws to third with nobody to play on", play.Caption);
        Assert.Contains("in at first", play.Caption);
    }

    // ---------------------------------------------------------------------------------
    // S-35  Bad chemistry: a share of throws slant past the cover — live, ERROR, extra bases
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S35_BadChemistryThrowsToFirstSailPastTheCoverAsLiveErrorsAcrossASeedSweep()
    {
        var errors = 0;
        var outs = 0;
        var extraBase = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            // SS rio throwing to 1B ashlord: authored rivals (bad chemistry).
            var match = Defense(leadoff: "cinder", first: "ashlord", shortstop: "rio", seed: seed);
            var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
            var preview = match.PreviewHit(hit);
            Assert.Equal("SS", preview.Position);
            var sailed = false;
            var (play, _, thrown) = RunCpu(match, hit, preview, out _, live => { if (live.Events.Contains(LiveEvent.ThrowSailed)) sailed = true; });
            Assert.NotNull(thrown);
            Assert.Equal(Chemistry.Bad, thrown!.Relation);
            if (play.Outcome!.Error)
            {
                errors++;
                Assert.True(sailed, "the ERROR is the throw that missed the cover (§8.5)");
                Assert.True(thrown.Slanted, "a slanted throw is what misses by more than the cover radius");
                Assert.Empty(play.Outcome.OutsMade);
                Assert.True(play.Outcome.BatterToBag >= 1);
                if (play.Outcome.BatterToBag >= 2) extraBase++;
                Assert.Equal(PlayStamp.Error, PlayStamp.Label(play));
            }
            else
            {
                // A sail that still ended in an out (the batter tagged trying for more) stamps OUT, not ERROR.
                if (sailed) Assert.NotEmpty(play.Outcome.OutsMade);
                if (play.Outcome.OutsMade.Count > 0) outs++;
            }
        }
        Assert.InRange(errors, 8, 40);
        Assert.True(outs > 0, "an ordinary bad-chemistry throw still retires the batter");
        // Whether the batter takes second on the overthrow is the runner's margin against the pickup
        // (§9.9), not a rule: a ball that skips a dozen feet past a first baseman standing there is a
        // hold; the count is reported, never asserted.
        _ = extraBase;
    }

    // ---------------------------------------------------------------------------------
    // §7 scenes
    // ---------------------------------------------------------------------------------

    [Fact]
    public void Scene72_SlowRollerIsFieldedInFrontOfThePlateAndThrownToFirst()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 38, 3, -6);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        Assert.True(preview.Position is "P" or "C" or "3B" or "1B", preview.Position);
        var (play, throwLandedAt, _) = RunCpu(match, hit, preview, out var threwTo);
        Assert.Equal(1, threwTo);
        Assert.True(play.Kind is PlayKind.GroundOut or PlayKind.Single, play.Kind.ToString());
        Assert.True(throwLandedAt > 0);
    }

    [Fact]
    public void Scene74_ChopperIsPlayedOffTheHopAndTheThrowRacesTheBatter()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        // A hard chop into the dirt in front of the plate: the high first bounce is the chopper (§6.2).
        var hit = FlightFixtures.Hit(match.Park, 105, -12, -12);
        Assert.Equal(BattedBallClass.Chopper, hit.Class);
        var preview = match.PreviewHit(hit);
        Assert.False(FieldingResolver.IsOutfield(preview.Position));
        var batter = match.Batter;
        var (play, throwLandedAt, _) = RunCpu(match, hit, preview, out var threwTo);
        Assert.True(play.Kind is PlayKind.GroundOut or PlayKind.Single, play.Kind.ToString());
        var body = Runner.BatterRunner(batter, HomeSet.BatterBodyX(batter.Bats), HomeSet.BatterZ);
        var batterAt = RunnerSystem.ArrivalSec(body, 1, 0, 0, match.Rules);
        if (play.Kind == PlayKind.GroundOut)
        {
            Assert.Equal(1, threwTo);
            Assert.True(throwLandedAt < batterAt, $"out only when the ball beat the body: {throwLandedAt:0.00} vs {batterAt:0.00}");
        }
        else
            Assert.True(threwTo == 0 || throwLandedAt >= batterAt, "safe only when the ball was late or never thrown");
    }

    [Fact]
    public void Scene75_ThroughTheInfieldTheRunnerFromFirstTakesWhatTheArmGives()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(1, 1);
        var match = scenario.Match;
        var runner = match.First!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 230, 8, 22);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        var throwsTo = new List<int>();
        var handedToOutfield = false;
        var (play, _, _) = RunCpu(match, hit, preview, out _, live =>
        {
            if (FieldingResolver.IsOutfield(live.GlovePos)) handedToOutfield = true;
            if (live.Throwing && (throwsTo.Count == 0 || throwsTo[^1] != live.ThrowBag)) throwsTo.Add(live.ThrowBag);
        });
        Assert.True(handedToOutfield, "the roll reaches the grass: the outfielder charges and takes the glove (§7.5)");
        Assert.Equal(PlayKind.Single, play.Kind);
        Assert.DoesNotContain(play.Outcome!.OutsMade, o => o.FromBag == 0);
        Assert.True(play.Outcome.BatterToBag >= 1);
        var move = play.Outcome.Moves.FirstOrDefault(m => m.Runner.Id == runner.Id);
        Assert.NotNull(move);
        Assert.True(move!.ToBag is 2 or 3, $"the runner from first is on second or third by the margin: {move.ToBag}");
        // The outfielder throws in (to a bag ahead of the lead runner or to the cutoff) so an infielder holds it for Time (§8.8, §10.6).
        Assert.NotEmpty(throwsTo);
    }

    [Fact]
    public void Scene76_LinerIsCaughtOnlyInsideTheShortWindowWhenTheRouteReachesIt()
    {
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Hit(match.Park, 100, 16, -8);
        Assert.Equal(BattedBallClass.Liner, hit.Class);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var start = Diamond.Positions[preview.Position];
        var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z,
            FieldingResolver.ChaseSpeedFt(map[preview.Position], preview.Frozen, match.Rules), match.Rules,
            match.Rules.Fielding.Reaction.LockoutSec(preview.Position));
        // The catch is the radius at the window (§8.3): a route that ends inside the glove's reach of the
        // plant by the end of the window (hang + windowAfterSec) is a catch.
        var window = FieldingResolver.CatchWindowFt(preview.CatchRadius, false, false, match.Rules);
        var lateFt = route.SpeedFtPerSec * match.Rules.Fielding.Catch.WindowAfterSec;
        var reaches = route.MissFt - lateFt < window;
        var catcherPos = "";
        var (play, _, _) = RunCpu(match, hit, preview, out _,
            live => { if (catcherPos == "" && live.Events.Contains(LiveEvent.Glove)) catcherPos = live.GlovePos; }, out var caughtAt);
        // A rope the infielder cannot reach may still be run down by the outfielder it hands off to
        // (§7.5); whoever takes it, the catch is only ever inside that glove's window (never force-fed).
        if (play.Kind == PlayKind.FlyOut)
        {
            Assert.NotEqual("", catcherPos);
            var who = map[catcherPos];
            Assert.True(FlyCatch.JumpWindow(caughtAt, preview.HangTimeSec, who, match.Park, match.Rules)
                        || FlyCatch.JumpWindow(caughtAt - Frame, preview.HangTimeSec, who, match.Park, match.Rules),
                $"{catcherPos} caught at {caughtAt:0.00} inside the window around {preview.HangTimeSec:0.00}");
        }
        else
            Assert.DoesNotContain(play.Outcome!.OutsMade, o => o.Type == OutType.Catch);
        if (reaches) Assert.Equal(PlayKind.FlyOut, play.Kind);
    }

    [Fact]
    public void Scene77_PopIsNeverThePitchersWhenAnotherGloveReachesAndTheRunnerHolds()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(2, 2);
        var match = scenario.Match;
        var runner = match.Second!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 110, 62, 6);
        Assert.Equal(BattedBallClass.Pop, hit.Class);
        var preview = match.PreviewHit(hit);
        Assert.NotEqual("P", preview.Position);
        var (play, _, _) = RunCpu(match, hit, preview, out _);
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal(OutType.Catch, only.Type);
        Assert.Equal(runner.Id, match.Second?.Id);
        Assert.DoesNotContain(play.Outcome.Moves, m => m.Runner.Id == runner.Id);
    }

    [Fact]
    public void Scene78_RoutineFlyIsCaughtInsideTheWindowNeverForceFedEarly()
    {
        var scenario = new Scenario(_content, seed: 2);
        var match = scenario.Match;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 262, 36, -10);
        var preview = match.PreviewHit(hit);
        Assert.True(FieldingResolver.IsOutfield(preview.Position));
        var (play, _, _) = RunCpu(match, hit, preview, out _, null, out var caughtAt);
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        var c = match.Rules.Fielding.Catch;
        var extra = FlyCatch.ExtraWindowSec(preview.Fielder, match.Park, match.Rules);
        Assert.True(caughtAt >= preview.HangTimeSec - c.WindowBeforeSec - extra - Frame, $"caught at {caughtAt:0.00}, hang {preview.HangTimeSec:0.00}");
        Assert.True(caughtAt <= preview.HangTimeSec + c.WindowAfterSec + extra * 0.5 + Frame);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Scene78_TheCatchingFielderStaysWhereTheCatchHappened(int outsBefore)
    {
        // #574: the body that caught the fly is where the catch was at Complete, on the typed outcome,
        // and stays there through a third out that flips the half (the sim's field resets; the bodies do not).
        var scenario = new Scenario(_content, seed: 2).Outs(outsBefore);
        var match = scenario.Match;
        var defenseBefore = match.DefenseRoster.Select(c => c.Id).ToHashSet();
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 262, 36, -10);
        var preview = match.PreviewHit(hit);
        Assert.True(FieldingResolver.IsOutfield(preview.Position));
        var catchAt = (X: double.NaN, Z: double.NaN);
        var catcherPos = "";
        var catcherId = "";
        // With nobody on, the catch is Time on the same tick and the sim resets its field inside that Apply
        // (the very frame the client used to mirror as a snap): the last live frame is the glove a step from the catch.
        var (play, _, _) = RunCpu(match, hit, preview, out _, live =>
        {
            if (!live.Active) return;
            catcherPos = live.GlovePos;
            catchAt = (live.GloveX, live.GloveZ);
            catcherId = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves)[catcherPos].Id;
        });
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        Assert.NotEqual("", catcherPos);
        var bodies = play.Outcome!.BodiesAtTime;
        Assert.Equal(9, bodies.Count(b => !b.IsRunner));
        Assert.All(bodies.Where(b => !b.IsRunner), b => Assert.Contains(b.Who.Id, defenseBefore));
        var catcher = Assert.Single(bodies, b => b.Pos == catcherPos);
        Assert.Equal(catcherId, catcher.Who.Id);
        Assert.True(Diamond.Dist(catcher.X, catcher.Z, catchAt.X, catchAt.Z) <= 3,
            $"{catcherPos} caught at ({catchAt.X:0},{catchAt.Z:0}) but stands at ({catcher.X:0},{catcher.Z:0}) at Time");
        var table = Diamond.Positions[catcherPos];
        Assert.True(Diamond.Dist(catcher.X, catcher.Z, table.X, table.Z) > 6,
            $"the fixture lands away from the table spot so a snap would show: {catcherPos} caught at ({catchAt.X:0},{catchAt.Z:0}), table ({table.X:0},{table.Z:0}), landing ({preview.LandingX:0},{preview.LandingZ:0})");
        if (outsBefore == 2)
        {
            Assert.NotEqual(play.Context!.Top, play.NextState!.Top);
            Assert.NotEqual(defenseBefore, match.DefenseRoster.Select(c => c.Id).ToHashSet());
        }
    }

    [Fact]
    public void Scene78_SacFlyIsALiveThrowHomeTheBodyRaces()
    {
        var scenario = new Scenario(_content, seed: 1).Runner(3, 2);
        var match = scenario.Match;
        var runner = match.Third!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 245, 34, -14);
        var preview = match.PreviewHit(hit);
        Assert.True(FieldingResolver.IsOutfield(preview.Position));
        var throwsTo = new List<int>();
        var tagged = false;
        var (play, _, _) = RunCpu(match, hit, preview, out _, live =>
        {
            if (live.Throwing && (throwsTo.Count == 0 || throwsTo[^1] != live.ThrowBag)) throwsTo.Add(live.ThrowBag);
            var third = match.RunnerAt(3);
            if (live.Fly == FlyState.Caught && third is { Live: true } && third.Feet > 0) tagged = true;
        });
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        Assert.True(tagged, "the runner on third tags on a deep fly with fewer than two outs (§7.8)");
        var scored = play.Outcome!.Moves.Any(m => m.Runner.Id == runner.Id && m.ToBag == 4);
        var outAtHome = play.Outcome.OutsMade.Any(o => o.Runner.Id == runner.Id && o.Type == OutType.Tag);
        Assert.True(scored ^ outAtHome, "the sac fly is a race: the run or the tag at the plate");
        Assert.Equal(scored ? 1 : 0, play.RunsScored);
        Assert.NotEmpty(throwsTo);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Home nine on defense (the top half): captain vale on the mound, ashlord at short by default,
    /// the rest a neutral royal / canopy / goldrush mix. The away nine's leadoff is the batter.
    /// </summary>
    Match Defense(string leadoff, string first = "lace", string shortstop = "ashlord", int seed = 1)
    {
        var home = _content.Team("Defense", "vale", "pewter", first, "frost", "basil", shortstop, "vine", "moss", "hex");
        var awayRest = new[] { "jester", "soot", "grit", "nugget", "boom", "marlow", "gull", "pip" }.Where(id => id != leadoff).Take(7).ToArray();
        var away = _content.Team("Offense", "zig", leadoff, awayRest[0], awayRest[1], awayRest[2], awayRest[3], awayRest[4], awayRest[5], awayRest[6]);
        var match = Match.Exhibition(_content, home, away, innings: 3, seed: seed);
        Assert.True(match.Top);
        Assert.Equal(leadoff, match.Batter.Id);
        Assert.Equal(shortstop, FieldingResolver.Assign(match.Defense, match.Pitcher)["SS"].Id);
        Assert.Equal(first, FieldingResolver.Assign(match.Defense, match.Pitcher)["1B"].Id);
        Assert.True(match.BeginAtBat(Paint, Swing, out _, out _), "the scripted swing must put the ball in play");
        return match;
    }

    (PlayEvent Play, double ThrowLandedAt, ThrowResult? Thrown) RunCpu(Match match, AtBatResult hit, FieldingPreview preview,
        out int firstThrowTo, Action<LivePlaySystem>? observe = null)
        => RunCpu(match, hit, preview, out firstThrowTo, observe, out _);

    (PlayEvent Play, double ThrowLandedAt, ThrowResult? Thrown) RunCpu(Match match, AtBatResult hit, FieldingPreview preview,
        out int firstThrowTo, Action<LivePlaySystem>? observe, out double caughtAtSink)
    {
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, field, LiveSeats.CpuOnly)).Snapshot.Active);
        PlayEvent? play = null;
        firstThrowTo = 0;
        var throwLandedAt = -1.0;
        var wasThrowing = false;
        ThrowResult? thrown = null;
        caughtAtSink = -1;
        for (var i = 0; i < 60 * 40 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame));
            if (caughtAtSink < 0 && (live.Caught || live.Events.Contains(LiveEvent.Glove)))
                caughtAtSink = live.ElapsedSeconds > 0 ? live.ElapsedSeconds : (i + 1) * Frame;
            if (live.Throwing && !wasThrowing)
            {
                if (firstThrowTo == 0) firstThrowTo = live.ThrowBag;
                thrown ??= live.ArmedThrow;
            }
            if (wasThrowing && !live.Throwing && throwLandedAt < 0)
                throwLandedAt = live.ElapsedSeconds > 0 ? live.ElapsedSeconds : (i + 1) * Frame;
            wasThrowing = live.Throwing;
            observe?.Invoke(live);
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        return (play!, throwLandedAt, thrown);
    }
}
