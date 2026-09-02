using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayCameraTests
{
    [Fact]
    public void SetIsMoundWhen1PPitchesPlateWhenBattingOr1v1()
    {
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 1));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 1, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 2, pitchingSet: true));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, true, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 2));
    }

    [Fact]
    public void Training1PFollowsTheRole()
    {
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, true, 0, 0, 0, training: true, seats: 1));
    }

    [Fact]
    public void InPlayTheaterDoesNotForkBySeatCount()
    {
        foreach (PlayCamera.Beat beat in Enum.GetValues<PlayCamera.Beat>())
        {
            if (beat is PlayCamera.Beat.Set or PlayCamera.Beat.PitchFlight) continue;
            var one = PlayCamera.Shot(beat, seats: 1);
            var two = PlayCamera.Shot(beat, seats: 2);
            Assert.Equal(one, two);
            Assert.False(string.IsNullOrWhiteSpace(one), beat.ToString());
        }
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Grounder));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.GrounderPull));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Line));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Fly));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Homer));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Smash));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Throw));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.StealThrow));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.Shot(PlayCamera.Beat.Tag));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.Shot(PlayCamera.Beat.Wall));
    }

    [Fact]
    public void FromHitMatchesTheaterShot()
    {
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        var pull = hopper with { SprayDeg = -20 };
        var fly = hopper with { LaunchDeg = 32, CarryFt = 280 };
        var homer = hopper with { LaunchDeg = 32, CarryFt = 420, HomeRun = true };
        var line = hopper with { LaunchDeg = 18, ExitVeloMph = 95, CarryFt = 180 };
        var star = hopper with { LaunchDeg = 28, StarSwingUsed = "heat-swing" };
        Assert.Equal(InPlay.TheaterShot(hopper), PlayCamera.FromHit(hopper));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(hopper));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(pull));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(line));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(fly));
        Assert.Equal(PlayCamera.InPlayFly, PlayCamera.FromHit(homer));
        Assert.Equal(PlayCamera.InPlay, PlayCamera.FromHit(star));
        Assert.Equal(PlayCamera.Beat.Homer, PlayCamera.BeatFrom(homer));
        Assert.Equal(PlayCamera.Beat.Fly, PlayCamera.BeatFrom(fly));
    }

    [Fact]
    public void FollowGroundLooksAtDirtNotTheAirborneBall()
    {
        var shot = new CameraShot(PlayCamera.InPlay, "ball", new Vec3(0, 54, -54), new Vec3(0, 0, 0), 50, 8);
        var air = new Vec3(40, 22, 90);
        var framed = PlayCamera.FollowGround(shot, air);
        Assert.Equal(PlayCamera.GroundUnder(air.X, air.Y, air.Z), framed.Look);
        Assert.Equal(0, framed.Look.Y);
        Assert.Equal(54, framed.Pos.Y);
        Assert.InRange(PlayCamera.LookDownDeg(shot), 44, 46);
        Assert.True(framed.Pos.Z < framed.Look.Z);
        Assert.Equal(air.X, framed.Look.X);
        Assert.Equal(air.X, framed.Pos.X);
        Assert.Equal(air.Z, framed.Look.Z);
    }

    [Fact]
    public void HudLayoutIsTheSameForOnePadAndTwo()
    {
        var one = BroadcastHud.Layout(1);
        var two = BroadcastHud.Layout(2);
        Assert.Equal(one, two);
        Assert.Equal(BroadcastHud.Standard, one);
        Assert.True(one.Score.X > 0.5, "score top-right");
        Assert.True(one.Score.Y < 0.2, "score top");
        Assert.True(one.BatterCard.X < 0.2, "batter bottom-left");
        Assert.True(one.BatterCard.Y > 0.7, "batter bottom");
        Assert.True(one.PitcherCard.X > 0.5, "pitcher bottom-right");
        Assert.True(one.PitcherCard.Y > 0.7, "pitcher bottom");
        Assert.True(one.MiniDiamond.X > 0.5, "diamond stays with the score cluster");
        Assert.True(one.Count.X > 0.5, "S/B/O stays with the score cluster");
        Assert.True(one.Banner.X > 0.2 && one.Banner.X + one.Banner.W < 0.8, "banner does not steal card corners");
        Assert.False(Overlaps(one.BatterCard, one.PitcherCard));
        Assert.False(Overlaps(one.Score, one.BatterCard));
        Assert.False(Overlaps(one.Score, one.PitcherCard));
        var px1 = one.BatterCard.Pixel(1920, 1080);
        var px2 = two.BatterCard.Pixel(1920, 1080);
        Assert.Equal(px1, px2);
    }

    static bool Overlaps(BroadcastHud.HudRect a, BroadcastHud.HudRect b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;
}
