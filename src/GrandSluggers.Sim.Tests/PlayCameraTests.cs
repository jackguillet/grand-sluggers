using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayCameraTests
{
    [Fact]
    public void SetIsMoundIn1PAndPlateIn1v1()
    {
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 1, pitchingSet: true));
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 1));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: 2, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.PitchFlight, seats: 2));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, true, 0, 0, 0, seats: 2));
    }

    [Fact]
    public void Training1PStaysBehindThePitcher()
    {
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(false, false, 0, 0, 0, training: true, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, true, 0, 0, 0, training: true, seats: 1));
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
        Assert.Equal("diamond-grounder", PlayCamera.Shot(PlayCamera.Beat.Grounder));
        Assert.Equal("diamond-pull", PlayCamera.Shot(PlayCamera.Beat.GrounderPull));
        Assert.Equal("diamond-line", PlayCamera.Shot(PlayCamera.Beat.Line));
        Assert.Equal("diamond", PlayCamera.Shot(PlayCamera.Beat.Fly));
        Assert.Equal("diamond-homer", PlayCamera.Shot(PlayCamera.Beat.Homer));
        Assert.Equal("smash", PlayCamera.Shot(PlayCamera.Beat.Smash));
        Assert.Equal("throw", PlayCamera.Shot(PlayCamera.Beat.Throw));
        Assert.Equal("throw", PlayCamera.Shot(PlayCamera.Beat.StealThrow));
        Assert.Equal("tag", PlayCamera.Shot(PlayCamera.Beat.Tag));
        Assert.Equal(PlayCamera.Wall, PlayCamera.Shot(PlayCamera.Beat.Wall));
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
        Assert.Equal("diamond-grounder", PlayCamera.FromHit(hopper));
        Assert.Equal("diamond-pull", PlayCamera.FromHit(pull));
        Assert.Equal("diamond-line", PlayCamera.FromHit(line));
        Assert.Equal("diamond", PlayCamera.FromHit(fly));
        Assert.Equal("diamond-homer", PlayCamera.FromHit(homer));
        Assert.Equal("smash", PlayCamera.FromHit(star));
        Assert.Equal(PlayCamera.Beat.Homer, PlayCamera.BeatFrom(homer));
        Assert.Equal(PlayCamera.Beat.Fly, PlayCamera.BeatFrom(fly));
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
