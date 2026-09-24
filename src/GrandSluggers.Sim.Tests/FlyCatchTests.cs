using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FlyCatchTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryCatchesDependOnPositionAndWindowNeverThrowButton(bool throwPressed)
    {
        Assert.True(FlyCatch.PlayerCaught(false, throwPressed, true, true, false));
        Assert.False(FlyCatch.PlayerCaught(false, throwPressed, false, true, false));
        Assert.False(FlyCatch.PlayerCaught(false, throwPressed, true, false, false));
        Assert.False(FlyCatch.PlayerCaught(false, throwPressed, true, true, true));
        Assert.True(FlyCatch.PlayerCaught(false, throwPressed, true, false, false, linerInAir: true));
    }

    [Fact]
    public void NearWallHomerCannotBeScoopedThroughTheFence()
    {
        var content = ContentCatalog.Load();
        foreach (var park in content.Parks.Values)
        {
            var match = Match.Slice(content, parkId: park.Id);
            var hit = FlightFixtures.Hit(park, 110, 35, 0, ContactQuality.Perfect);
            var pre = match.PreviewHit(hit);
            foreach (var spray in new[] { -35d, 0d, 35d })
            {
                var wall = AtBatResolver.FenceAt(park, spray);
                var outside = BallFlight.GroundPoint(wall + 1, spray);
                Assert.False(FlyCatch.PickupInPlay(pre with { Class = BattedBallClass.Grounder }, park,
                    outside.X, outside.Z, 1, pre.HangTimeSec, rules: Rules.Default));
                Assert.False(FlyCatch.PickupInPlay(pre with { Class = BattedBallClass.Liner }, park,
                    outside.X, outside.Z, 1, pre.HangTimeSec, rules: Rules.Default));
                Assert.False(FlyCatch.TouchScoop(pre, park, outside.X, outside.Z, 0,
                    pre.HangTimeSec + 1, pre.HangTimeSec, 9, 20, rules: Rules.Default));
                var inside = BallFlight.GroundPoint(wall - 1, spray);
                Assert.True(FlyCatch.TouchScoop(pre, park, inside.X, inside.Z, 0,
                    pre.HangTimeSec + 1, pre.HangTimeSec, 7, 20, rules: Rules.Default));
                Assert.False(FlyCatch.TouchScoop(pre, park, inside.X, inside.Z, 2,
                    pre.HangTimeSec - 0.1, pre.HangTimeSec, 7, 20, rules: Rules.Default));
            }
        }
    }

    [Fact]
    public void HarborNearWallFlightMeetsTheWallAndStaysInThePark()
    {
        var content = ContentCatalog.Load();
        var match = Match.Slice(content);
        var park = match.Park;
        // In the open this carries a few feet past Harbor's 400; in the park it meets the 8-ft wall below the top (§6.1).
        // The C80 copy: the fence is 280 and the drag is 0.0040, so 110 mph dies at 277. 111.7 mph carries 281, a foot past the fence.
        var exit = 111.7;
        Assert.True(BallFlight.CarryFeet(exit, 35, 0, rules: Rules.Default) > park.CenterFenceFt, "in the open this lands past Harbor's 400");
        var ball = BattedBall.Of(exit, 35, 0, park, rules: Rules.Default);
        Assert.Equal(BattedBallClass.Wall, ball.Class);
        Assert.False(ball.HomeRun);
        Assert.NotNull(ball.WallT);
        Assert.True(ball.FenceClearFt < 0, $"met the wall {ball.FenceClearFt:0.0} ft over the top");
        Assert.InRange(ball.LandingDist, park.CenterFenceFt - 1, park.CenterFenceFt + 0.5);
        Assert.All(ball.Samples, sample => Assert.True(sample.Dist <= park.CenterFenceFt + 0.5, $"sample {sample.T:0.00} at {sample.Dist:0.0} is through the wall"));
        var afterWall = ball.Samples.Where(s => s.T > ball.WallT + 0.5).ToArray();
        Assert.NotEmpty(afterWall);
        Assert.All(afterWall, s => Assert.True(s.Dist < park.CenterFenceFt - 1, "the carom comes back into the park"));
        var hit = FlightFixtures.Hit(park, exit, 35, 0, ContactQuality.Perfect);
        var pre = match.PreviewHit(hit);
        Assert.Equal(BattedBallClass.Wall, pre.Class);
        var plant = FlyCatch.ChaseTarget(pre, Rules.Default, park);
        Assert.True(Diamond.Dist(0, 0, plant.X, plant.Z) < park.CenterFenceFt, "the glove plants inside the wall");
    }

    [Fact]
    public void PlayerJumpInWindowTakesAFlySouthLateDoesNot()
    {
        var rio = _content.Must("rio");
        var pop = Pop();
        var pre = Routine(rio);
        Assert.False(FlyCatch.NeedsJump(pre));
        Assert.True(FlyCatch.IsFly(pre));
        Assert.True(FlyCatch.JumpWindow(pre.HangTimeSec - 0.2, pre.HangTimeSec, Rules.Default, rio, Harbor));
        Assert.True(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: true, needsJump: false));
        Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, pre));

        Assert.False(FlyCatch.JumpWindow(pre.HangTimeSec + 0.4, pre.HangTimeSec, Rules.Default, rio, Harbor), "jump late");
        Assert.False(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: false, needsJump: false));
        // A ball that falls in is live: the bodies name the hit at Complete, never the carry (§10.6).
        Assert.Equal(PlayKind.InPlay, FlyCatch.PlayerKind(false, pre));
        Assert.Equal(PlayKind.InPlay, FlyCatch.PlayerKind(true, pre, inAir: false));
        _ = pop;
        Assert.False(FlyCatch.PlayerCaught(jumpDown: false, southDown: true, under: true, inWindow: false, needsJump: false),
            "a throw press cannot take an ineligible fly");
        Assert.True(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: true, needsJump: false),
            "the leap stays armed after the press");
        Assert.True(FlyCatch.PlayerDiveCatch(true, distFt: 16, standUpFt: 10, diveWindowFt: 20, ballY: 2, rules: Rules.Default));
        Assert.False(FlyCatch.PlayerDiveCatch(false, distFt: 16, standUpFt: 10, diveWindowFt: 20, ballY: 2, rules: Rules.Default));
        Assert.False(FlyCatch.PlayerDiveCatch(true, distFt: 30, standUpFt: 10, diveWindowFt: 20, ballY: 2, rules: Rules.Default));
        Assert.False(FlyCatch.PlayerDiveCatch(true, distFt: 9, standUpFt: 10, diveWindowFt: 20, ballY: 2, rules: Rules.Default),
            "a plant catch is stand-up even if East is armed");
    }

    [Fact]
    public void PlayerRobRequiresJumpInWindowSouthIsNotARob()
    {
        var rio = _content.Must("rio");
        var hr = Homer();
        var pre = Wall(rio);
        Assert.True(FlyCatch.NeedsJump(pre));
        Assert.False(FlyCatch.PlayerCaught(jumpDown: false, southDown: true, under: true, inWindow: true, needsJump: true),
            "South does not scoop a would-be homer");
        Assert.True(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: true, needsJump: true));
        Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, pre));
        Assert.False(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: false, needsJump: true),
            "jump late is a homer");
        Assert.Equal(PlayKind.HomeRun, FlyCatch.PlayerKind(false, pre));
        _ = hr;
    }

    [Fact]
    public void CatchAtThePlantIsStandUpARimCatchIsADive()
    {
        var c = Rules.Default.Fielding.Catch;
        Assert.Equal(1, c.WindowPadFt);
        Assert.Equal(2, c.DiveReachFt);
        Assert.Equal(7.5, c.DiveMaxBallY);
        // The C80 copy (#719): every unauthored body stands up at the table's one reach, catch.standUpReachFt 6.0, not at
        // radiusBaseFt + Field x radiusPerField. The ring, the rim and the dive past it are the same rule on both tables.
        Assert.Equal(4.0, c.StandUpReachFt);
        var rio = _content.Must("rio");
        var ashlord = _content.Must("ashlord");
        var park = Harbor;
        var rioRadius = FieldingResolver.CatchRadiusFt(rio, park, rules: Rules.Default);
        var ashRadius = FieldingResolver.CatchRadiusFt(ashlord, park, rules: Rules.Default);
        Assert.Equal(c.StandUpReachFt + FieldAbilities.CatchBonus(rio, rules: Rules.Default), rioRadius);
        Assert.Equal(c.StandUpReachFt, ashRadius);
        var standUp = FieldingResolver.StandUpCatchFt(rioRadius);
        var diveWin = FieldingResolver.DiveCatchFt(rioRadius, rules: Rules.Default);
        Assert.Equal(rioRadius, standUp);
        Assert.Equal(rioRadius + c.DiveReachFt, diveWin);
        Assert.True(FieldingResolver.CatchWindowFt(rioRadius, false, false, rules: Rules.Default) > standUp,
            "windowPad is dirt scoop slack, not a stand-up fly out");

        var plant = 0.0;
        Assert.True(FlyCatch.AutoCatch(under: plant < standUp, inWindow: true, needsJump: false));
        Assert.Equal(DefensiveFeat.None, FieldingResolver.PlayerCatchFeat(
            FlightFixtures.Preview(rio, "CF", BattedBallClass.Fly, 2.8, 0, 240), park, Rules.Default, false, false, dived: false));

        var rim = standUp + c.DiveReachFt * 0.5;
        Assert.True(rim >= standUp && rim < diveWin);
        Assert.False(FlyCatch.AutoCatch(under: rim < standUp, inWindow: true, needsJump: false),
            "past the ring is not a stand-up");
        Assert.True(FlyCatch.PlayerDiveCatch(true, rim, standUp, diveWin, ballY: 2, rules: Rules.Default));
        Assert.False(FlyCatch.PlayerDiveCatch(true, rim, standUp, diveWin, ballY: 8, rules: Rules.Default));
        var fly = FlightFixtures.Preview(rio, "CF", BattedBallClass.Fly, 2.8, 0, 240);
        Assert.Equal(DefensiveFeat.Dive, FieldingResolver.PlayerCatchFeat(fly, park, Rules.Default, false, false, dived: true));
        Assert.Equal("DIVE", PlayStamp.Label(PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.Dive));

        var past = diveWin + 0.1;
        Assert.False(FlyCatch.AutoCatch(under: past < standUp, inWindow: true, needsJump: false));
        Assert.False(FlyCatch.NeedsDive(past, standUp, diveWin, ballY: 2, rules: Rules.Default));
        Assert.Equal(fly.CatchRadius, LandingMark.RadiusFt(fly));
    }

    [Fact]
    public void CpuDeadStickStillCatchesARoutineFly()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry, rules: Rules.Default);
        var pop = FlightFixtures.Hit(match.Park, 88, 32, 0);
        Assert.Equal(BattedBallClass.Fly, pop.Class);
        var pre = fielding.Preview(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.HomeRunLikely);
        Assert.False(FlyCatch.NeedsJump(pre));
        // The resolver no longer predicts the catch (§8.3): the ball is in play until the glove takes it.
        var field = fielding.Resolve(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1), pre: pre);
        Assert.Equal(PlayKind.InPlay, field.Kind);
        Assert.NotNull(field.Fielder);

        var plant = FlyCatch.ChaseTarget(pre, Rules.Default, match.Park);
        Assert.True(FlyCatch.Under(plant.X, plant.Z, ballX: 0, ballZ: plant.Z - 40, plant.X, plant.Z, 22, needsJump: false, rules: Rules.Default),
            "standing in the landing ring is under — live XZ still short is not a drop");
        Assert.True(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: false));
        Assert.False(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: true), "dead-stick does not rob");
        Assert.False(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: true, canRob: false), "the CPU leap needs the rob height");
        Assert.True(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: true, canRob: true), "the CPU leap at the wall is geometric (§8.3)");
        Assert.True(FlyCatch.AutoCatch(under: true, inWindow: false, needsJump: false, linerInAir: true),
            "a liner on the glove before the bounce is a catch, not a hang-window plant");
        Assert.False(FlyCatch.AutoCatch(under: true, inWindow: false, needsJump: false, linerInAir: false),
            "a fly still needs the window");

        var start = Diamond.Positions[pre.Position];
        var hang = Math.Max(0.8, pre.HangTimeSec);
        var run = FieldingResolver.ChaseSpeedFt(pre.Fielder, frozen: false, rules: Rules.Default);
        var at = start;
        const double dt = 1.0 / 30;
        for (var t = 0.0; t < hang - 0.18; t += dt)
        {
            at = FieldingResolver.StepToward(at.X, at.Z, plant.X, plant.Z, run, dt, Rules.Default, match.Park);
        }
        Assert.True(Diamond.Dist(at.X, at.Z, plant.X, plant.Z) < 18,
            "the selected routine glove reaches the ring at rated speed by hang");
    }

    [Fact]
    public void StandingOnTheBallScoopsWithoutSouthOrStick()
    {
        const double window = 14;
        Assert.True(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: 0.4, rules: Rules.Default),
            "standing on a hopper scoops it");
        Assert.True(FlyCatch.TouchScoop(distFt: 13.9, windowFt: window, ballY: 3.1, rules: Rules.Default));
        Assert.False(FlyCatch.TouchScoop(distFt: 14, windowFt: window, ballY: 0.4, rules: Rules.Default),
            "outside the glove is not a pickup");
        Assert.False(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: 18, rules: Rules.Default),
            "a fly still up is not a pickup");
        Assert.False(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: Rules.Default.Fielding.Catch.TouchScoopY, rules: Rules.Default));
    }

    [Fact]
    public void ALinerOnTheGloveBeforeTheBounceIsACatchTheDirtIsAScoop()
    {
        var rio = _content.Must("rio");
        var liner = FlightFixtures.Preview(rio, "SS", BattedBallClass.Liner, hang: 1.4, x: -48, z: 150, radius: 14);
        const double window = 16;
        var plantX = liner.LandingX;
        var plantZ = liner.LandingZ;
        var minY = Rules.Default.Fielding.Catch.InAirMinY;
        // On the rope, short of the bounce: South / AutoCatch. Not the landing ring.
        Assert.True(FlyCatch.InPosition(liner, gloveX: 4, gloveZ: 90, ballX: 5, ballZ: 92, ballY: 6,
            plantX, plantZ, window, hitT: 0.7, hangSec: liner.HangTimeSec, needsJump: false, rules: Rules.Default),
            "a liner is held on the live ball (§7.6), not only under the bounce");
        Assert.False(FlyCatch.Under(4, 90, 5, 92, plantX, plantZ, window, needsJump: false, rules: Rules.Default),
            "the plant is the bounce; the intercept is not there");
        Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, liner, inAir: true));
        Assert.True(FlyCatch.PlayerCaught(jumpDown: false, southDown: true, under: true, inWindow: false, needsJump: false, linerInAir: true),
            "a straight-at-you liner is a positional catch");
        // Already bounced: a scoop, never a silent catch.
        Assert.False(FlyCatch.InPosition(liner, gloveX: 4, gloveZ: 90, ballX: 5, ballZ: 92, ballY: 6,
            plantX, plantZ, window, hitT: liner.HangTimeSec + 0.05, hangSec: liner.HangTimeSec, needsJump: false, rules: Rules.Default));
        Assert.True(FlyCatch.InPosition(liner, gloveX: 4, gloveZ: 90, ballX: 5, ballZ: 92, ballY: minY,
            plantX, plantZ, window, hitT: 0.7, hangSec: liner.HangTimeSec, needsJump: false, rules: Rules.Default),
            "a low ball before first ground contact is still a catch");
        Assert.Equal(PlayKind.InPlay, FlyCatch.PlayerKind(true, liner, inAir: false));
        var fly = Routine(rio);
        Assert.False(FlyCatch.InPosition(fly, gloveX: 4, gloveZ: 90, ballX: 5, ballZ: 92, ballY: 18,
            fly.LandingX, fly.LandingZ, window, hitT: 0.4, hangSec: fly.HangTimeSec, needsJump: false, rules: Rules.Default),
            "an overhead fly is outside the standing envelope");
        Assert.False(FlyCatch.InPosition(fly, gloveX: fly.LandingX, gloveZ: fly.LandingZ, ballX: 0, ballZ: 40, ballY: 18,
            fly.LandingX, fly.LandingZ, window, hitT: fly.HangTimeSec - 0.2, hangSec: fly.HangTimeSec, needsJump: false, rules: Rules.Default));
    }

    [Fact]
    public void SuperJumpWidensTheWindowItDoesNotSkipIt()
    {
        var nico = _content.Must("nico");
        var rio = _content.Must("rio");
        Assert.Equal("super-jump", nico.FieldAbility);
        var hang = 3.2;
        var early = hang - Rules.Default.Fielding.Catch.WindowBeforeSec - 0.10;
        Assert.False(FlyCatch.JumpWindow(early, hang, Rules.Default, rio, Harbor));
        Assert.True(FlyCatch.JumpWindow(early, hang, Rules.Default, nico, Harbor));
        Assert.False(FlyCatch.JumpWindow(hang + 0.5, hang, Rules.Default, nico, Harbor), "late is still late");
        var tenOver = FlightFixtures.OverTheFence(Harbor, 10, 0);
        Assert.True(FieldAbilities.AirRob(Harbor, nico, tenOver, rules: Rules.Default));
        Assert.False(FieldAbilities.AirRob(Harbor, rio, tenOver, rules: Rules.Default));
        Assert.False(FieldAbilities.AirRob(Harbor, nico, FlightFixtures.OverTheFence(Harbor, 25, 0), rules: Rules.Default), "past the rob height it is gone");
    }

    [Fact]
    public void LiveBeatRisesWithAHomerThenSitsOnTheWall()
    {
        var rio = _content.Must("rio");
        var hr = Homer();
        var pre = Wall(rio);
        var hang = 3.2;
        Assert.Equal(PlayCamera.Beat.Homer, FlyCatch.LiveBeat(hr, pre, 0.2, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(hr, pre, 0.2, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.Beat.Wall, FlyCatch.LiveBeat(hr, pre, hang - 0.4, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, rules: Rules.Default));
        Assert.Equal(
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 1, rules: Rules.Default),
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 2, rules: Rules.Default));
        var pop = Pop();
        var routine = Routine(rio);
        Assert.Equal(PlayCamera.Beat.Fly, FlyCatch.LiveBeat(pop, routine, hang - 0.4, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(pop, routine, hang - 0.4, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.Beat.Fly, FlyCatch.LiveBeat(pop, routine, 0.1, hang, false, rules: Rules.Default));
        // #665: a liner is not OnTheDirt and not the fly pull-back.
        var linerHit = new AtBatResult(ContactQuality.Nice, true, false, 95, 16, 180, false, false, null, null,
            SprayDeg: 6, Class: BattedBallClass.Liner);
        var linerPre = FlightFixtures.Preview(rio, "SS", BattedBallClass.Liner, 1.1, 20, 110);
        Assert.Equal(PlayCamera.Beat.Line, FlyCatch.LiveBeat(linerHit, linerPre, 0.2, 1.1, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.InPlayLine, FlyCatch.LiveShot(linerHit, linerPre, 0.2, 1.1, false, rules: Rules.Default));
        Assert.NotEqual(PlayCamera.InPlay, FlyCatch.LiveShot(linerHit, linerPre, 0.2, 1.1, false, rules: Rules.Default));
        Assert.NotEqual(PlayCamera.InPlayFly, FlyCatch.LiveShot(linerHit, linerPre, 0.2, 1.1, false, rules: Rules.Default));
        Assert.Equal(
            FlyCatch.LiveShot(linerHit, linerPre, 0.2, 1.1, false, seats: 1, rules: Rules.Default),
            FlyCatch.LiveShot(linerHit, linerPre, 0.2, 1.1, false, seats: 2, rules: Rules.Default));
        // A star swing follows its class (§15): the smash beat is the home run's, timed by the client's smashHold.
        var smash = hr with { StarSwingUsed = "heat-swing" };
        Assert.Equal(PlayCamera.BeatFrom(hr), PlayCamera.BeatFrom(smash));
        Assert.Equal(PlayCamera.Beat.Homer, FlyCatch.LiveBeat(smash, pre, 0.2, hang, false, rules: Rules.Default));
        Assert.Equal(PlayCamera.Beat.Wall, FlyCatch.LiveBeat(smash, pre, hang - 0.4, hang, false, rules: Rules.Default));
    }

    [Fact]
    public void AWallBallFollowsTheBallOnTheFlyShot()
    {
        // D14 / #608: the wall beat is the fly follow on the dirt under the ball, not a glove cam that swoops in.
        var fly = _content.Shots.Must(PlayCamera.InPlayFly);
        var at = new Vec3(12, 5.5, 310);
        var framed = PlayCamera.FollowGround(fly, at);
        Assert.Equal(PlayCamera.InPlayFly, framed.Shot);
        Assert.Equal(new Vec3(at.X, 0, at.Z), framed.Look);
        Assert.Equal(fly.Fov, framed.Fov);
        var one = PlayCamera.Shot(PlayCamera.Beat.Wall, seats: 1);
        var two = PlayCamera.Shot(PlayCamera.Beat.Wall, seats: 2);
        Assert.Equal(one, two);
        Assert.Equal(PlayCamera.InPlayFly, one);
    }

    [Fact]
    public void WallPlantSitsInsideTheHarborFence()
    {
        var rio = _content.Must("rio");
        var hr = Homer();
        var pre = Wall(rio);
        var plant = FlyCatch.WallPlant(pre, Rules.Default, Harbor);
        var dist = Math.Sqrt(plant.X * plant.X + plant.Z * plant.Z);
        Assert.True(dist < AtBatResolver.FenceAt(Harbor, 0), $"plant {dist} past fence");
        Assert.Equal(plant, FlyCatch.ChaseTarget(pre, Rules.Default, Harbor));
        var pop = Pop();
        var routine = Routine(rio);
        Assert.Equal((routine.LandingX, routine.LandingZ), FlyCatch.ChaseTarget(routine, Rules.Default, Harbor));
    }

    [Fact]
    public void LandingMarkIsACircleOnTheGrassWhileTheBallIsInTheAir()
    {
        var rio = _content.Must("rio");
        var fly = Routine(rio);
        Assert.True(LandingMark.On(fly, ballY: 18, hitT: 0.4, caught: false, buddy: false, rules: Rules.Default));
        Assert.False(LandingMark.On(fly, ballY: 18, hitT: 0.4, caught: true, buddy: false, rules: Rules.Default));
        Assert.False(LandingMark.On(fly, ballY: 0.2, hitT: fly.HangTimeSec + 0.3, caught: false, buddy: false, rules: Rules.Default));
        var plant = LandingMark.At(fly, Rules.Default, Harbor);
        Assert.Equal((fly.LandingX, fly.LandingZ), plant);
        Assert.Equal(fly.CatchRadius, LandingMark.RadiusFt(fly));
        Assert.True(LandingMark.RadiusFt(fly) >= Rules.Default.Fielding.Catch.StandUpReachFt);
        Assert.True(LandingMark.WorldY > LandingMark.DirtY);
        Assert.True(LandingMark.ThickFt > 0.4, "tube must read from the fly 3/4, not a pancake");
        Assert.False(LandingMark.Hot(0.2, fly.HangTimeSec, Rules.Default, rio, Harbor));
        Assert.True(LandingMark.Hot(fly.HangTimeSec - 0.2, fly.HangTimeSec, Rules.Default, rio, Harbor));

        var liner = FlightFixtures.Preview(rio, "SS", BattedBallClass.Liner, 1.1, 20, 110, radius: 12);
        Assert.True(LandingMark.On(liner, ballY: 7, hitT: 0.2, caught: false, buddy: false, rules: Rules.Default),
            "a liner still up gets the circle — it looks like a fly");
        var hopper = FlightFixtures.Preview(rio, "SS", BattedBallClass.Grounder, 0.6, 12, 70, radius: 12);
        Assert.False(LandingMark.On(hopper, ballY: 3, hitT: 0.1, caught: false, buddy: false, rules: Rules.Default),
            "a hopper has no circle — they chase the live hop");
        var wall = Wall(rio);
        Assert.Equal(FlyCatch.WallPlant(wall, Rules.Default, Harbor), LandingMark.At(wall, Rules.Default, Harbor));
    }

    [Fact]
    public void BuddyJumpOfferStillNeedsTwoGoodChemOutfieldersUnderAHomer()
    {
        var dart = _content.Must("dart");
        var zig = _content.Must("zig");
        var offered = FlightFixtures.Preview(dart, "CF", BattedBallClass.Homer, 4.2, 0, 390, zig);
        Assert.True(FieldingResolver.BuddyJumpOffered(offered));
        Assert.True(FlyCatch.NeedsJump(offered));
    }

    Park Harbor => _content.Parks["harbor-diamond"];

    static AtBatResult Pop() =>
        new(ContactQuality.Nice, true, false, 88, 32, 240, false, false, null, null, SprayDeg: 0);

    static AtBatResult Homer() =>
        new(ContactQuality.Perfect, true, false, 100, 28, 420, true, false, null, null, SprayDeg: 0);

    static FieldingPreview Routine(Character who) =>
        FlightFixtures.Preview(who, "CF", BattedBallClass.Fly, 2.8, 0, 240);

    static FieldingPreview Wall(Character who) =>
        FlightFixtures.Preview(who, "CF", BattedBallClass.Homer, 4.2, 0, 420);
}
