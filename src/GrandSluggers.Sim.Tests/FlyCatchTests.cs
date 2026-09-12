using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FlyCatchTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void NearWallHomerCannotBeScoopedThroughTheFence()
    {
        var content = ContentCatalog.Load();
        foreach (var park in content.Parks.Values)
        {
            var match = Match.Slice(content, parkId: park.Id);
            var hit = new AtBatResult(ContactQuality.Perfect, true, false, 110, 35,
                BallFlight.CarryFeet(110, 35, park.WindMph), true, false, null, null);
            var pre = match.PreviewHit(hit);
            foreach (var spray in new[] { -35d, 0d, 35d })
            {
                var wall = AtBatResolver.FenceAt(park, spray);
                var outside = BallFlight.GroundPoint(wall + 1, spray);
                Assert.False(FlyCatch.PickupInPlay(pre with { Grounder = true }, park,
                    outside.X, outside.Z, 1, pre.HangTimeSec));
                Assert.False(FlyCatch.PickupInPlay(pre with { Line = true }, park,
                    outside.X, outside.Z, 1, pre.HangTimeSec));
                Assert.False(FlyCatch.TouchScoop(pre, park, outside.X, outside.Z, 0,
                    pre.HangTimeSec + 1, pre.HangTimeSec, 9, 20));
                var inside = BallFlight.GroundPoint(wall - 1, spray);
                Assert.True(FlyCatch.TouchScoop(pre, park, inside.X, inside.Z, 0,
                    pre.HangTimeSec + 1, pre.HangTimeSec, 7, 20));
                Assert.False(FlyCatch.TouchScoop(pre, park, inside.X, inside.Z, 2,
                    pre.HangTimeSec - 0.1, pre.HangTimeSec, 7, 20));
            }
        }
    }

    [Fact]
    public void HarborNearWallFlightNeverBecomesAPickupAfterCrossing()
    {
        var content = ContentCatalog.Load();
        var match = Match.Slice(content);
        var park = match.Park;
        var path = BallFlight.Trajectory(110, 35, park.WindMph);
        var hang = BallFlight.HangTime(path);
        var carry = BallFlight.CarryFeet(110, 35, park.WindMph);
        Assert.InRange(carry, 404, 405); // Four feet past Harbor's center fence.
        var hit = new AtBatResult(ContactQuality.Perfect, true, false, 110, 35, carry, true, false, null, null);
        var pre = match.PreviewHit(hit);
        var plant = FlyCatch.WallPlant(pre, park);
        var outside = path.Where(sample => sample.Dist > park.CenterFenceFt).ToArray();
        Assert.NotEmpty(outside);
        Assert.Contains(outside, sample => FlyCatch.TouchScoop(Math.Abs(sample.Dist - plant.Z), 20, sample.Height));
        Assert.All(outside, sample => Assert.False(FlyCatch.TouchScoop(pre, park, 0, sample.Dist,
            sample.Height, sample.T, hang, Math.Abs(sample.Dist - plant.Z), 20)));
    }

    [Fact]
    public void PlayerJumpInWindowTakesAFlySouthLateDoesNot()
    {
        var rio = _content.Must("rio");
        var pop = Pop();
        var pre = Routine(rio);
        Assert.False(FlyCatch.NeedsJump(pre));
        Assert.True(FlyCatch.IsFly(pre));
        Assert.True(FlyCatch.JumpWindow(pre.HangTimeSec - 0.2, pre.HangTimeSec, rio, Harbor));
        Assert.True(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: true, needsJump: false));
        Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, pre, pop));

        Assert.False(FlyCatch.JumpWindow(pre.HangTimeSec + 0.4, pre.HangTimeSec, rio, Harbor), "jump late");
        Assert.False(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: false, needsJump: false));
        Assert.Equal(PlayKind.Single, FlyCatch.PlayerKind(false, pre, pop));
        var gap = pop with { CarryFt = 260 };
        Assert.Equal(PlayKind.Double, FlyCatch.PlayerKind(true, pre, gap, inAir: false));
        Assert.True(FlyCatch.PlayerCaught(jumpDown: false, southDown: true, under: true, inWindow: false, needsJump: false),
            "South still scoops a routine fly you are under");
        Assert.True(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: true, needsJump: false),
            "the leap stays armed after the press");
        Assert.True(FlyCatch.PlayerDiveCatch(true, distFt: 16, windowFt: 20, ballY: 2));
        Assert.False(FlyCatch.PlayerDiveCatch(false, distFt: 16, windowFt: 20, ballY: 2));
        Assert.False(FlyCatch.PlayerDiveCatch(true, distFt: 30, windowFt: 20, ballY: 2));
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
        Assert.Equal(PlayKind.FlyOut, FlyCatch.PlayerKind(true, pre, hr));
        Assert.False(FlyCatch.PlayerCaught(jumpDown: true, southDown: false, under: true, inWindow: false, needsJump: true),
            "jump late is a homer");
        Assert.Equal(PlayKind.HomeRun, FlyCatch.PlayerKind(false, pre, hr));
    }

    [Fact]
    public void CpuDeadStickStillCatchesARoutineFly()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielding = new FieldingResolver(_content.Chemistry);
        var pop = new AtBatResult(ContactQuality.Nice, true, false, 88, 32, 280, false, false, null, null, SprayDeg: 0);
        Assert.False(FieldingResolver.IsGrounder(pop));
        Assert.False(FieldingResolver.IsLine(pop));
        var pre = fielding.Preview(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.HomeRunLikely);
        Assert.False(FlyCatch.NeedsJump(pre));
        var field = fielding.Resolve(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1), pre: pre);
        Assert.Equal(PlayKind.FlyOut, field.Kind);
        Assert.NotNull(field.Fielder);

        var plant = FlyCatch.ChaseTarget(pre, match.Park);
        Assert.True(FlyCatch.Under(plant.X, plant.Z, ballX: 0, ballZ: plant.Z - 40, plant.X, plant.Z, 22, needsJump: false),
            "standing in the landing ring is under — live XZ still short is not a drop");
        Assert.True(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: false));
        Assert.False(FlyCatch.AutoCatch(under: true, inWindow: true, needsJump: true), "dead-stick does not rob");

        var start = Diamond.Positions[pre.Position];
        var hang = Math.Max(0.8, pre.HangTimeSec);
        var run = FieldingResolver.ChaseSpeedFt(pre.Fielder, frozen: false);
        var at = start;
        const double dt = 1.0 / 30;
        for (var t = 0.0; t < hang - 0.18; t += dt)
        {
            at = FieldingResolver.StepToward(at.X, at.Z, plant.X, plant.Z, run, dt, match.Park);
        }
        Assert.True(Diamond.Dist(at.X, at.Z, plant.X, plant.Z) < 18,
            "the selected routine glove reaches the ring at rated speed by hang");
    }

    [Fact]
    public void StandingOnTheBallScoopsWithoutSouthOrStick()
    {
        const double window = 14;
        Assert.True(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: 0.4),
            "standing on a hopper scoops it");
        Assert.True(FlyCatch.TouchScoop(distFt: 13.9, windowFt: window, ballY: 3.1));
        Assert.False(FlyCatch.TouchScoop(distFt: 14, windowFt: window, ballY: 0.4),
            "outside the glove is not a pickup");
        Assert.False(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: 18),
            "a fly still up is not a pickup");
        Assert.False(FlyCatch.TouchScoop(distFt: 2, windowFt: window, ballY: Rules.Default.Fielding.Catch.TouchScoopY));
    }

    [Fact]
    public void SuperJumpWidensTheWindowItDoesNotSkipIt()
    {
        var nico = _content.Must("nico");
        var rio = _content.Must("rio");
        Assert.Equal("super-jump", nico.FieldAbility);
        var hang = 3.2;
        var early = hang - Rules.Default.Fielding.Catch.WindowBeforeSec - 0.10;
        Assert.False(FlyCatch.JumpWindow(early, hang, rio, Harbor));
        Assert.True(FlyCatch.JumpWindow(early, hang, nico, Harbor));
        Assert.False(FlyCatch.JumpWindow(hang + 0.5, hang, nico, Harbor), "late is still late");
        Assert.True(FieldAbilities.AirRob(Harbor, nico, Homer() with { CarryFt = AtBatResolver.FenceAt(Harbor, 0) + 10 }));
        Assert.False(FieldAbilities.AirRob(Harbor, rio, Homer() with { CarryFt = AtBatResolver.FenceAt(Harbor, 0) + 10 }));
    }

    [Fact]
    public void LiveBeatRisesWithAHomerThenSitsOnTheWall()
    {
        var rio = _content.Must("rio");
        var hr = Homer();
        var pre = Wall(rio);
        var hang = 3.2;
        Assert.Equal(PlayCamera.Beat.Homer, FlyCatch.LiveBeat(hr, pre, 0.2, hang, false));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(hr, pre, 0.2, hang, false));
        Assert.Equal(PlayCamera.Beat.Wall, FlyCatch.LiveBeat(hr, pre, hang - 0.4, hang, false));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false));
        Assert.Equal(
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 1),
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 2));
        var pop = Pop();
        var routine = Routine(rio);
        Assert.Equal(PlayCamera.Beat.Fly, FlyCatch.LiveBeat(pop, routine, hang - 0.4, hang, false));
        Assert.Equal(PlayCamera.InPlayFly, FlyCatch.LiveShot(pop, routine, hang - 0.4, hang, false));
        Assert.Equal(PlayCamera.Beat.Fly, FlyCatch.LiveBeat(pop, routine, 0.1, hang, false));
        var smash = hr with { StarSwingUsed = "heat-swing" };
        Assert.Equal(PlayCamera.Beat.Smash, PlayCamera.BeatFrom(smash));
        Assert.Equal(PlayCamera.Beat.Homer, FlyCatch.LiveBeat(smash, pre, 0.2, hang, false));
        Assert.Equal(PlayCamera.Beat.Wall, FlyCatch.LiveBeat(smash, pre, hang - 0.4, hang, false));
    }

    [Fact]
    public void FollowPutsTheWallShotOnTheGlove()
    {
        var wall = _content.Shots.Must(PlayCamera.Wall);
        Assert.Equal("glove", wall.Look, ignoreCase: true);
        var at = new Vec3(12, 5.5, 310);
        var framed = PlayCamera.Follow(wall, at);
        Assert.Equal(PlayCamera.Wall, framed.Shot);
        Assert.Equal(at, framed.Look);
        Assert.InRange(framed.Pos.X - at.X, 20, 28);
        Assert.InRange(framed.Pos.Z - at.Z, -38, -28);
        Assert.Equal(wall.Fov, framed.Fov);
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
        var plant = FlyCatch.WallPlant(pre, Harbor);
        var dist = Math.Sqrt(plant.X * plant.X + plant.Z * plant.Z);
        Assert.True(dist < AtBatResolver.FenceAt(Harbor, 0), $"plant {dist} past fence");
        Assert.Equal(plant, FlyCatch.ChaseTarget(pre, Harbor));
        var pop = Pop();
        var routine = Routine(rio);
        Assert.Equal((routine.LandingX, routine.LandingZ), FlyCatch.ChaseTarget(routine, Harbor));
    }

    [Fact]
    public void LandingMarkIsACircleOnTheGrassWhileTheBallIsInTheAir()
    {
        var rio = _content.Must("rio");
        var fly = Routine(rio);
        Assert.True(LandingMark.On(fly, ballY: 18, hitT: 0.4, caught: false, buddy: false));
        Assert.False(LandingMark.On(fly, ballY: 18, hitT: 0.4, caught: true, buddy: false));
        Assert.False(LandingMark.On(fly, ballY: 0.2, hitT: fly.HangTimeSec + 0.3, caught: false, buddy: false));
        var plant = LandingMark.At(fly, Harbor);
        Assert.Equal((fly.LandingX, fly.LandingZ), plant);
        Assert.True(LandingMark.RadiusFt(fly) >= LandingMark.MinRadiusFt);
        Assert.True(LandingMark.WorldY > LandingMark.DirtY);
        Assert.True(LandingMark.ThickFt > 0.4, "tube must read from the fly 3/4, not a pancake");
        Assert.False(LandingMark.Hot(0.2, fly.HangTimeSec, rio, Harbor));
        Assert.True(LandingMark.Hot(fly.HangTimeSec - 0.2, fly.HangTimeSec, rio, Harbor));

        var liner = new FieldingPreview(rio, "SS", null, 1.1, 20, 110, false, false, false, false, false, 12, Line: true);
        Assert.True(LandingMark.On(liner, ballY: 7, hitT: 0.2, caught: false, buddy: false),
            "a liner still up gets the circle — it looks like a fly");
        var hopper = new FieldingPreview(rio, "SS", null, 0.6, 12, 70, true, false, false, false, false, 12);
        Assert.False(LandingMark.On(hopper, ballY: 3, hitT: 0.1, caught: false, buddy: false),
            "a hopper has no circle — they chase the live hop");
        var wall = Wall(rio);
        Assert.Equal(FlyCatch.WallPlant(wall, Harbor), LandingMark.At(wall, Harbor));
    }

    [Fact]
    public void BuddyJumpOfferStillNeedsTwoGoodChemOutfieldersUnderAHomer()
    {
        var dart = _content.Must("dart");
        var zig = _content.Must("zig");
        var offered = new FieldingPreview(dart, "CF", zig, 4.2, 0, 390, false, true, false, false, false, 14);
        Assert.True(FieldingResolver.BuddyJumpOffered(offered));
        Assert.True(FlyCatch.NeedsJump(offered));
    }

    Park Harbor => _content.Parks["harbor-diamond"];

    static AtBatResult Pop() =>
        new(ContactQuality.Nice, true, false, 88, 32, 240, false, false, null, null, SprayDeg: 0);

    static AtBatResult Homer() =>
        new(ContactQuality.Perfect, true, false, 100, 28, 420, true, false, null, null, SprayDeg: 0);

    static FieldingPreview Routine(Character who) =>
        new(who, "CF", null, 2.8, 0, 240, false, false, false, false, false, 14);

    static FieldingPreview Wall(Character who) =>
        new(who, "CF", null, 4.2, 0, 420, false, true, false, false, false, 14);
}
