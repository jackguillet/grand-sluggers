using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class InPlayTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void EnergyScalesWithExitAndQuality()
    {
        var soft = Hit(ContactQuality.Sour, 60);
        var hard = Hit(ContactQuality.Perfect, 100);
        Assert.True(InPlay.Energy(hard) > InPlay.Energy(soft),
            $"hard {InPlay.Energy(hard)} vs soft {InPlay.Energy(soft)}");
    }

    [Fact]
    public void HighEnergyBobblesMoreThanADyingRoller()
    {
        var rio = _content.Must("rio");
        var hard = Hit(ContactQuality.Perfect, 110);
        var dying = Hit(ContactQuality.Sour, 40);
        var hardN = 0;
        var dyingN = 0;
        const int n = 80;
        for (var i = 0; i < n; i++)
        {
            if (InPlay.Bobbles(InPlay.Energy(hard), rio, new Random(i))) hardN++;
            if (InPlay.Bobbles(InPlay.Energy(dying), rio, new Random(i))) dyingN++;
        }
        Assert.True(hardN > dyingN, $"hard bobbles {hardN} vs dying {dyingN}");
        Assert.Equal(0, dyingN);
    }

    [Fact]
    public void OneSpeedFormulaForEveryRunnerAndTheBatterStartsLate()
    {
        // Spec §9.1: bagSec = 3.55 − Run × 0.12 clamped, one formula for every segment; the batter
        // leaves the box 0.5 s after contact; a left-handed batter's box is closer to first.
        var dart = _content.Must("dart");
        var brick = _content.Must("brondo");
        Assert.True(RunnerSystem.BagSec(dart) < RunnerSystem.BagSec(brick), $"dart {RunnerSystem.BagSec(dart)} vs brondo {RunnerSystem.BagSec(brick)}");
        var s = Rules.Default.Running.BagSec;
        Assert.Equal(Math.Clamp(s.BaseSec - dart.Stats.Run * s.SecPerRun, s.MinSec, s.MaxSec), RunnerSystem.BagSec(dart), 6);
        var righty = Runner.BatterRunner(dart, HomeSet.BatterBodyX(Hand.R), HomeSet.BatterZ);
        var lefty = Runner.BatterRunner(dart, HomeSet.BatterBodyX(Hand.L), HomeSet.BatterZ);
        var toFirstR = RunnerSystem.ArrivalSec(righty, 1, 0);
        var toFirstL = RunnerSystem.ArrivalSec(lefty, 1, 0);
        Assert.True(toFirstL < toFirstR, $"lefty {toFirstL} vs righty {toFirstR}");
        Assert.InRange(toFirstR - s.BatterStartSec, RunnerSystem.BagSec(dart) * 0.9, RunnerSystem.BagSec(dart) * 1.1);
        var seated = new Runner(dart, 1);
        Assert.Equal(RunnerSystem.BagSec(dart), RunnerSystem.ArrivalSec(seated, 2, 0), 6);
        Assert.Equal(2 * RunnerSystem.BagSec(dart), RunnerSystem.ArrivalSec(seated, 3, 0), 6);
        Assert.Equal(0, RunnerSystem.ArrivalSec(seated, 1, 0));
    }

    [Fact]
    public void DashShortensTheRaceToFirstWithoutTeleporting()
    {
        var dart = _content.Must("dart");
        var body = Runner.BatterRunner(dart, HomeSet.BatterBodyX(Hand.R), HomeSet.BatterZ);
        var still = RunnerSystem.ArrivalSec(body, 1, 0, 0);
        var dash = RunnerSystem.ArrivalSec(body, 1, 0, 1);
        Assert.True(dash < still, $"dash {dash} vs {still}");
        Assert.True(still - dash < 0.6, "dash is not a teleport");
        // A throw that lands between the two: out standing still, in on the mash.
        var throwSec = (still + dash) / 2;
        Assert.True(throwSec < still && throwSec > dash);
    }

    [Fact]
    public void ScoopMissIsASingleNotASilentGroundOut()
    {
        var match = Match.Slice(_content, seed: 4);
        var fielding = new FieldingResolver(_content.Chemistry);
        // Deep hopper: landing is past the infield so the nearest glove cannot scoop it.
        var hit = FlightFixtures.Landing(match.Park, 300, 8, 2);
        var rng = new Random(4);
        var pre = fielding.Preview(hit, match.Park, match.Defense.Roster, match.Pitcher, rng);
        Assert.True(pre.Grounder, "launch 8 must be a hopper");
        var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, rng, pre: pre);
        Assert.Equal(PlayKind.Single, field.Kind);
        Assert.NotEqual(PlayKind.GroundOut, field.Kind);
    }

    [Fact]
    public void GroundThrowBagsForceThenFirstOnADoublePlayRace()
    {
        Assert.Equal(new[] { 2, 1 }, InPlay.GroundThrowBags(true, false));
        Assert.Equal(new[] { 2 }, InPlay.GroundThrowBags(true, true));
        Assert.Equal(new[] { 1 }, InPlay.GroundThrowBags(false, false));
        Assert.Empty(InPlay.GroundThrowBags(false, true));
        // First empty: the tag bag only when that runner is going (S-36); the batter at first is the second throw.
        Assert.Equal(new[] { 3, 1 }, InPlay.GroundThrowBags(false, secondGoing: true, thirdGoing: false, batterBeatsThrow: false));
        Assert.Equal(new[] { 4, 1 }, InPlay.GroundThrowBags(false, secondGoing: true, thirdGoing: true, batterBeatsThrow: false));
        Assert.Equal(new[] { 1 }, InPlay.GroundThrowBags(false, secondGoing: false, thirdGoing: false, batterBeatsThrow: false));
        Assert.Equal(new[] { 3 }, InPlay.GroundThrowBags(false, secondGoing: true, thirdGoing: false, batterBeatsThrow: true));
        Assert.Equal(2, InPlay.DefaultGroundBag(true));
        Assert.Equal(1, InPlay.DefaultGroundBag(false));
        Assert.Equal(3, InPlay.DefaultGroundBag(false, true, false));
        Assert.Equal(4, InPlay.DefaultGroundBag(false, true, true));
        Assert.Equal(1, InPlay.NextBagAfterForce(2, 1));
        Assert.Equal(0, InPlay.NextBagAfterForce(2, 3));
        Assert.True(InPlay.DoublePlayOffered(true, 0));
        Assert.True(InPlay.DoublePlayOffered(true, 1));
        Assert.False(InPlay.DoublePlayOffered(true, 2));
        Assert.False(InPlay.DoublePlayOffered(false, 0));
        Assert.True(FieldingResolver.DoublePlayHopper(true, true, 0));
        Assert.False(FieldingResolver.DoublePlayHopper(false, true, 0));
        Assert.Equal(2, InPlay.CommitBag(0, hopperCaught: true, cutoff: false, defaultBag: 2));
        Assert.Equal(1, InPlay.CommitBag(0, hopperCaught: true, cutoff: false, defaultBag: 1));
        Assert.Equal(0, InPlay.CommitBag(0, hopperCaught: true, cutoff: true, defaultBag: 2));
        Assert.Equal(3, InPlay.CommitBag(3, hopperCaught: true, cutoff: false, defaultBag: 2));
    }

    [Fact]
    public void ThrowToBagStepsForceThenFirstWithoutCollapsingThePlay()
    {
        var force = InPlay.ThrowToBag(2, true, false, runnerBeats: false, outs: 0, "Vale", "Rio");
        Assert.True(force.Out);
        Assert.True(force.Force);
        Assert.False(force.TurnedTwo);
        Assert.False(force.PlayOver);
        Assert.Equal(1, force.NextDefaultBag);
        Assert.Contains("forces the runner", force.Caption);

        var two = InPlay.ThrowToBag(1, false, alreadyForced: true, runnerBeats: false, outs: 1, "Vale", "Rio");
        Assert.True(two.Out);
        Assert.True(two.TurnedTwo);
        Assert.True(two.PlayOver);
        Assert.Contains("turns two", two.Caption);

        var late = InPlay.ThrowToBag(1, false, alreadyForced: true, runnerBeats: true, outs: 1, "Vale", "Rio");
        Assert.False(late.Out);
        Assert.False(late.TurnedTwo);
        Assert.True(late.BatterSafe);
        Assert.Contains("Force at second", late.Caption);
        Assert.Contains("Rio", late.Caption);

        var thirdOut = InPlay.ThrowToBag(2, true, false, runnerBeats: false, outs: 2, "Vale", "Rio");
        Assert.True(thirdOut.Out);
        Assert.True(thirdOut.PlayOver);
        Assert.Equal(0, thirdOut.NextDefaultBag);

        var first = InPlay.ThrowToBag(1, false, alreadyForced: false, runnerBeats: false, outs: 0, "Vale", "Rio");
        Assert.True(first.Out);
        Assert.False(first.Force);
        Assert.False(first.PlayOver, "outs < 3: Time decides, remaining runners may still be live");
        Assert.Contains("first", first.Caption);

        var beat = InPlay.ThrowToBag(1, false, alreadyForced: false, runnerBeats: true, outs: 0, "Vale", "Rio");
        Assert.False(beat.Out);
        Assert.True(beat.BatterSafe);
    }

    [Fact]
    public void ForceStateFollowsOccupancyAndClearsAheadWhenATrailerIsOut()
    {
        var empty = InPlay.ForceState.FromOccupancy(false, false, false);
        Assert.True(empty.At(1));
        Assert.False(empty.At(2));
        Assert.False(empty.At(3));
        Assert.False(empty.At(4));

        var first = InPlay.ForceState.FromOccupancy(true, false, false);
        Assert.True(first.At(2));
        Assert.False(first.At(3));

        var corner = InPlay.ForceState.FromOccupancy(true, true, false);
        Assert.True(corner.At(3));
        Assert.False(corner.At(4));

        var loaded = InPlay.ForceState.FromOccupancy(true, true, true);
        Assert.True(loaded.At(4));
        Assert.False(loaded.AfterOutAt(1).At(2));
        Assert.False(loaded.AfterOutAt(1).At(4));
        Assert.True(loaded.AfterOutAt(2).At(1));
        Assert.False(loaded.AfterOutAt(2).At(3));
        Assert.False(loaded.AfterOutAt(2).At(4));
        Assert.True(loaded.AfterOutAt(4).At(2));
        Assert.False(loaded.AfterOutAt(4).At(4));
    }

    [Fact]
    public void ThrowToBagForcesThirdAndHomeAndTagsWhenItIsNotAForce()
    {
        var corner = InPlay.ForceState.FromOccupancy(true, true, false);
        var forceThird = InPlay.ThrowToBag(3, corner, true, runnerBeats: false, 0, false, "Vale", "Rio");
        Assert.True(forceThird.Out);
        Assert.True(forceThird.Force);
        Assert.Contains("third", forceThird.Caption, StringComparison.OrdinalIgnoreCase);

        var late = InPlay.ThrowToBag(3, corner, true, runnerBeats: true, 0, false, "Vale", "Rio");
        Assert.False(late.Out);
        Assert.Contains("beats", late.Caption, StringComparison.OrdinalIgnoreCase);

        var loaded = InPlay.ForceState.FromOccupancy(true, true, true);
        var home = InPlay.ThrowToBag(4, loaded, true, runnerBeats: false, 0, false, "Vale", "Rio");
        Assert.True(home.Out);
        Assert.True(home.Force);
        Assert.Contains("home", home.Caption, StringComparison.OrdinalIgnoreCase);

        var tagOnly = InPlay.ForceState.FromOccupancy(false, true, false);
        var tag = InPlay.ThrowToBag(3, tagOnly, true, runnerBeats: false, 0, false, "Vale", "Rio");
        Assert.True(tag.Out);
        Assert.False(tag.Force);
        Assert.Contains("tags", tag.Caption, StringComparison.OrdinalIgnoreCase);

        var nobody = InPlay.ThrowToBag(3, tagOnly, runnerPresent: false, runnerBeats: false, 0, false, "Vale", "Rio");
        Assert.False(nobody.Out);

        var afterBatter = loaded.AfterOutAt(1);
        var noForceSecond = InPlay.ThrowToBag(2, afterBatter, true, runnerBeats: false, 1, false, "Vale", "Rio");
        Assert.True(noForceSecond.Out);
        Assert.False(noForceSecond.Force, "batter out at first: second is a tag");
    }

    [Fact]
    public void HopperCatchStickDeadThrowsToFirstDiamondIsDistinct()
    {
        Assert.False(InPlay.StickNamesBag(chasing: true, caught: false));
        Assert.False(InPlay.StickNamesBag(chasing: false, caught: true));
        Assert.True(InPlay.StickNamesBag(chasing: false, caught: false));
        Assert.Equal(0, InPlay.DiamondBag(0, 0));
        Assert.Equal(1, InPlay.DiamondBag(1, 0));
        Assert.Equal(2, InPlay.DiamondBag(0, 1));
        Assert.Equal(3, InPlay.DiamondBag(-1, 0));
        Assert.Equal(4, InPlay.DiamondBag(0, -1));
        Assert.Equal(0, InPlay.ArmedBag(0, InPlay.DiamondBag(1, 0), stickOk: false));
        Assert.Equal(1, InPlay.ArmedBag(0, InPlay.DiamondBag(1, 0), stickOk: true));
        Assert.Equal(1, InPlay.ArmedBag(1, 2, stickOk: true));
        Assert.Equal(2, InPlay.ArmedBag(2, 1, stickOk: false));
        Assert.Equal(3, InPlay.ArmedBag(3, 0, stickOk: false));
        Assert.Equal(4, InPlay.ArmedBag(4, 0, stickOk: true));
        Assert.Equal(1, InPlay.CommitBag(0, hopperCaught: true, cutoff: false));
        Assert.Equal(0, InPlay.CommitBag(0, hopperCaught: true, cutoff: true));
        Assert.Equal(2, InPlay.CommitBag(2, hopperCaught: true, cutoff: false));
        Assert.Equal(4, InPlay.CommitBag(4, hopperCaught: true, cutoff: true));
        Assert.Equal(0, InPlay.CommitBag(0, hopperCaught: false, cutoff: false));
    }

    [Fact]
    public void HopperWithoutPlayerThrowIsAPlayEvent()
    {
        var match = Match.Slice(_content, seed: 1);
        var hopper = new AtBatResult(ContactQuality.Nice, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        Assert.True(InPlay.FairContactSendsBatter(hopper));
        var field = match.ResolveFielding(hopper);
        Assert.True(field.Kind is PlayKind.GroundOut or PlayKind.Single or PlayKind.FlyOut, field.Kind.ToString());
        var pitch = new PitchCommand("fastball", 0, false);
        var swing = new SwingCommand(true, 0, 0, false, LaunchAim: 0.6);
        Assert.True(match.BeginAtBat(pitch, swing, out var hit, out _));
        var ev = match.FinishAtBat(pitch, swing, hit, field);
        Assert.True(ev.Kind is PlayKind.GroundOut or PlayKind.Single or PlayKind.FlyOut or PlayKind.Double,
            ev.Kind.ToString());
        Assert.False(string.IsNullOrWhiteSpace(ev.Caption));
    }

    [Fact]
    public void HardHopperCanBobbleIntoASingle()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var hit = new AtBatResult(ContactQuality.Perfect, true, false, 110, 8, 45, false, false, null, null, SprayDeg: 2);
        var bobbles = 0;
        var outs = 0;
        for (var i = 0; i < 80; i++)
        {
            var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, new Random(i));
            if (field.Bobble)
            {
                bobbles++;
                Assert.Equal(PlayKind.Single, field.Kind);
                Assert.Equal(0, field.KnockbackSec);
            }
            else if (field.Kind == PlayKind.GroundOut)
            {
                outs++;
                Assert.False(field.Bobble);
                Assert.True(field.KnockbackSec > 0, "a 110 mph perfect hopper must shove the fielder");
            }
        }
        Assert.True(bobbles > 0, "a rocket at the shins must eat someone in 80 tries");
        Assert.True(outs > 0, "the same rocket is still an out when the glove holds");
    }

    [Fact]
    public void DyingRollerDoesNotBobbleOrKnockBack()
    {
        var match = Match.Slice(_content, seed: 2);
        var fielding = new FieldingResolver(_content.Chemistry);
        var hit = new AtBatResult(ContactQuality.Sour, true, false, 40, 6, 30, false, false, null, null, SprayDeg: 0);
        for (var i = 0; i < 40; i++)
        {
            var field = fielding.Resolve(hit, match.Park, match.Defense.Roster, match.Pitcher, new Random(i));
            Assert.False(field.Bobble);
            Assert.Equal(0, field.KnockbackSec);
        }
    }

    [Fact]
    public void TheaterShotIsOneHighDiamond()
    {
        var hopper = new AtBatResult(ContactQuality.Nice, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        var pull = hopper with { SprayDeg = -20 };
        var fly = hopper with { LaunchDeg = 32, CarryFt = 280 };
        var homer = hopper with { LaunchDeg = 32, CarryFt = 420, HomeRun = true };
        var line = hopper with { LaunchDeg = 18, ExitVeloMph = 95, CarryFt = 180 };
        var star = hopper with { LaunchDeg = 28, StarSwingUsed = "heat-swing" };
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(hopper));
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(pull));
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(line));
        Assert.Equal(PlayCamera.InPlayFly, InPlay.TheaterShot(fly));
        Assert.Equal(PlayCamera.InPlayFly, InPlay.TheaterShot(homer));
        Assert.Equal(PlayCamera.InPlay, InPlay.TheaterShot(star));
        Assert.Equal(BattedBallClass.Liner, BattedBallClasses.ByLaunch(line.LaunchDeg, line.ExitVeloMph));
        Assert.True(BattedBallClasses.ByLaunch(hopper.LaunchDeg, hopper.ExitVeloMph).OnTheDirt());
        Assert.False(BattedBallClasses.ByLaunch(fly.LaunchDeg, fly.ExitVeloMph).OnTheDirt());
    }

    [Fact]
    public void TimeWaitsUntilEveryRunnerHasBeenOnABagASecond()
    {
        // Spec §10.6: three outs; or an infielder holds the ball unthrown while every live runner has
        // stood on a bag for timeOnBagSec. The bodies are what Time reads.
        Assert.Equal(1.0, Rules.Default.Running.Bags.TimeOnBagSec);
        var rio = _content.Must("rio");
        var batter = Runner.BatterRunner(rio, HomeSet.BatterX, HomeSet.BatterZ);
        var runners = new[] { batter };
        Assert.True(InPlay.Time(true, false, 3, true, runners), "three outs is Time");
        Assert.False(InPlay.Time(false, false, 0, true, runners), "no ball is not Time");
        Assert.False(InPlay.Time(true, true, 0, true, runners), "a throw is not Time");
        Assert.False(InPlay.Time(true, false, 0, true, runners), "batter between bags");
        var t = 0.0;
        const double dt = 1.0 / 60;
        while (!batter.IsOn(1))
        {
            RunnerSystem.Tick(runners, dt, new RunnerTickContext(t, 0, FlyState.None, 0, _ => false, _ => false));
            t += dt;
            Assert.True(t < 10, "the batter reaches first");
        }
        var expected = Rules.Default.Running.BagSec.BatterStartSec + batter.SegmentFt / RunnerSystem.SpeedFtPerSec(rio);
        Assert.InRange(t, expected - 0.05, expected + 0.05);
        Assert.False(InPlay.Time(true, false, 0, true, runners), "batter just arrived");
        for (var i = 0; i < 63; i++)
            RunnerSystem.Tick(runners, dt, new RunnerTickContext(t += dt, 0, FlyState.None, 0, _ => false, _ => false));
        Assert.True(InPlay.Time(true, false, 0, true, runners));
        Assert.False(InPlay.Time(true, false, 0, false, runners), "an outfielder holding it on the grass is not Time (§10.6)");

        var third = new Runner(_content.Must("vale"), 3);
        var two = new[] { batter, third };
        Assert.False(InPlay.Time(true, false, 0, true, two), "a runner just seated has not settled");
        for (var i = 0; i < 63; i++)
            RunnerSystem.Tick(two, dt, new RunnerTickContext(t += dt, 0, FlyState.None, 0, _ => false, _ => false));
        Assert.True(InPlay.Time(true, false, 0, true, two));
        batter.Retire();
        Assert.True(InPlay.Time(true, false, 1, true, two), "an out is not a live runner");
        third.Send(4);
        RunnerSystem.Tick(two, dt, new RunnerTickContext(t += dt, 0, FlyState.None, 1, _ => false, _ => false));
        Assert.False(InPlay.Time(true, false, 1, true, two), "a runner going home keeps the play alive");
        var mid = InPlay.AlongBases(Diamond.Baseline * 0.5, 2);
        Assert.False(InPlay.OccupyingBag(mid.X, mid.Z), "halfway to first is not a bag");
        var atTwo = InPlay.TowardBag(0, 2, Diamond.Baseline * 2);
        Assert.True(InPlay.OccupyingBag(atTwo.X, atTwo.Z), "double dest is second");
        Assert.Equal(Diamond.Second.X, atTwo.X, 1);
    }

    [Fact]
    public void TouchesIsATagOffTheBag()
    {
        var midPath = InPlay.AlongBases(Diamond.Baseline * 0.5, 1);
        Assert.False(InPlay.OccupyingBag(midPath.X, midPath.Z, Rules.Default.Running.Bags.TagSafeRadiusFt));
        Assert.True(InPlay.Touches(true, false, midPath.X, midPath.Z, midPath.X, midPath.Z));
        Assert.False(InPlay.Touches(true, false, midPath.X, midPath.Z, midPath.X, midPath.Z, runnerOnBag: true),
            "on a bag they are safe");
        Assert.False(InPlay.Touches(false, false, midPath.X, midPath.Z, midPath.X, midPath.Z), "no ball");
        Assert.False(InPlay.Touches(true, true, midPath.X, midPath.Z, midPath.X, midPath.Z), "throwing");
        var first = Diamond.First;
        Assert.True(InPlay.OccupyingBag(first.X, first.Z, Rules.Default.Running.Bags.TagSafeRadiusFt));
        Assert.False(InPlay.Touches(true, false, first.X, first.Z, first.X, first.Z),
            "standing on first is not a tag");
        Assert.True(InPlay.CloseSafe(3.2, 3.1), "a step ahead of the throw is SAFE");
        Assert.False(InPlay.CloseSafe(3.0, 3.1), "throw beats the runner");
        Assert.False(InPlay.CloseSafe(4.5, 3.1), "waiting on the bag is not bang-bang");
        var off = InPlay.AlongBases(Diamond.Baseline * 0.2, 1);
        Assert.False(InPlay.OccupyingBag(off.X, off.Z, Rules.Default.Running.Bags.TagSafeRadiusFt), "off home toward first");
        Assert.True(InPlay.Touches(true, false, off.X + 10, off.Z, off.X, off.Z),
            "toy bodies overlap from the diamond camera");
        var stepOffFirst = InPlay.AlongBases(Diamond.Baseline - 8, 1);
        Assert.False(InPlay.OccupyingBag(stepOffFirst.X, stepOffFirst.Z, Rules.Default.Running.Bags.TagSafeRadiusFt),
            "a step off first is a tag");
        Assert.True(InPlay.Touches(true, false, stepOffFirst.X, stepOffFirst.Z, stepOffFirst.X, stepOffFirst.Z));
        Assert.False(InPlay.Touches(true, false, 0, 0, midPath.X, midPath.Z), "too far");
        Assert.True(Rules.Default.Running.Bags.TagSafeRadiusFt < Rules.Default.Running.Bags.OccupyRadiusFt);
        Assert.True(Rules.Default.Running.Bags.TagSafeRadiusFt < Rules.Default.Running.Bags.TagReachFt);
    }

    [Fact]
    public void SteppingOnFirstWithTheBallIsTheForceOut()
    {
        var first = Diamond.First;
        var halfway = InPlay.AlongBases(Diamond.Baseline * 0.5, 1);
        Assert.True(InPlay.OnThisBag(1, first.X, first.Z));
        Assert.False(InPlay.OnThisBag(1, halfway.X, halfway.Z));
        Assert.True(InPlay.ForceAtBag(1, false, false, false));
        Assert.False(InPlay.ForceAtBag(2, false, false, false));
        Assert.True(InPlay.ForceAtBag(2, true, false, false));
        Assert.True(InPlay.ForceAtBag(3, true, true, false));
        Assert.True(InPlay.ForceAtBag(4, true, true, true));
        Assert.False(InPlay.ForceAtBag(3, true, false, false));

        Assert.True(InPlay.ForceOnBag(true, 1, true, false, first.X, first.Z, halfway.X, halfway.Z),
            "1B on the bag, batter still coming: out");
        Assert.False(InPlay.ForceOnBag(true, 1, true, false, first.X, first.Z, first.X, first.Z),
            "batter already on first is safe");
        Assert.False(InPlay.ForceOnBag(true, 1, true, false, halfway.X, halfway.Z, halfway.X, halfway.Z),
            "glove off the bag is not a force");
        Assert.False(InPlay.ForceOnBag(false, 1, true, false, first.X, first.Z, halfway.X, halfway.Z));
        Assert.False(InPlay.ForceOnBag(true, 1, false, false, first.X, first.Z, halfway.X, halfway.Z),
            "no ball");
        Assert.False(InPlay.ForceOnBag(true, 1, true, true, first.X, first.Z, halfway.X, halfway.Z),
            "throwing");
        var second = Diamond.Second;
        var leavingFirst = InPlay.TowardBag(1, 2, 20);
        Assert.True(InPlay.ForceOnBag(true, 2, true, false, second.X, second.Z, leavingFirst.X, leavingFirst.Z),
            "2B on second, runner from first still coming");
    }

    static AtBatResult Hit(ContactQuality q, double exit, double launch = 22, double carry = 200) =>
        new(q, true, false, exit, launch, carry, false, false, null, null);
}
