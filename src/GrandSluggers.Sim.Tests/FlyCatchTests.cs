using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FlyCatchTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

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
        Assert.True(FlyCatch.PlayerCaught(jumpDown: false, southDown: true, under: true, inWindow: false, needsJump: false),
            "South still scoops a routine fly you are under");
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
        var pop = new AtBatResult(ContactQuality.Solid, true, false, 88, 32, 280, false, false, null, null, SprayDeg: 0);
        Assert.False(FieldingResolver.IsGrounder(pop));
        Assert.False(FieldingResolver.IsLine(pop));
        var pre = fielding.Preview(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1));
        Assert.False(pre.HomeRunLikely);
        Assert.False(FlyCatch.NeedsJump(pre));
        var field = fielding.Resolve(pop, match.Park, match.Defense.Roster, match.Pitcher, new Random(1), pre: pre);
        Assert.Equal(PlayKind.FlyOut, field.Kind);
        Assert.NotNull(field.Fielder);
    }

    [Fact]
    public void SuperJumpWidensTheWindowItDoesNotSkipIt()
    {
        var nico = _content.Must("nico");
        var rio = _content.Must("rio");
        Assert.Equal("super-jump", nico.FieldAbility);
        var hang = 3.2;
        var early = hang - FlyCatch.WindowBeforeSec - 0.10;
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
        Assert.Equal("diamond-homer", FlyCatch.LiveShot(hr, pre, 0.2, hang, false));
        Assert.Equal(PlayCamera.Beat.Wall, FlyCatch.LiveBeat(hr, pre, hang - 0.4, hang, false));
        Assert.Equal(PlayCamera.Wall, FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false));
        Assert.Equal(
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 1),
            FlyCatch.LiveShot(hr, pre, hang - 0.4, hang, false, seats: 2));
        var pop = Pop();
        var routine = Routine(rio);
        Assert.Equal(PlayCamera.Beat.Fly, FlyCatch.LiveBeat(pop, routine, hang - 0.4, hang, false));
        Assert.Equal("diamond", FlyCatch.LiveShot(pop, routine, hang - 0.4, hang, false));
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
        Assert.Equal(PlayCamera.Wall, one);
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
        new(ContactQuality.Solid, true, false, 88, 32, 240, false, false, null, null, SprayDeg: 0);

    static AtBatResult Homer() =>
        new(ContactQuality.Perfect, true, false, 100, 28, 420, true, false, null, null, SprayDeg: 0);

    static FieldingPreview Routine(Character who) =>
        new(who, "CF", null, 2.8, 0, 240, false, false, false, false, false, 14);

    static FieldingPreview Wall(Character who) =>
        new(who, "CF", null, 4.2, 0, 420, false, true, false, false, false, 14);
}
