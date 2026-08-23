using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class MatchTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void SliceGameFinishes()
    {
        var match = Match.Slice(_content, innings: 3, seed: 7);
        match.AutoPlayGame();
        Assert.True(match.Over);
        Assert.True(match.Log.Count > 10);
        Assert.InRange(match.Inning, 3, 5);
        var mvp = match.Mvp();
        Assert.False(string.IsNullOrWhiteSpace(mvp.Who.Name));
    }

    [Fact]
    public void FourBallsIsAWalk()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        PlayKind last = PlayKind.TakeBall;
        for (var i = 0; i < 4; i++)
            last = match.Play(wild, take).Kind;
        Assert.Equal(PlayKind.Walk, last);
        Assert.NotNull(match.First);
    }

    [Fact]
    public void ThreeLookingStrikesIsAStrikeout()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        PlayKind last = PlayKind.TakeStrike;
        for (var i = 0; i < 3; i++)
            last = match.Play(paint, take).Kind;
        Assert.Equal(PlayKind.Strikeout, last);
        Assert.Equal(1, match.Outs);
    }

    [Fact]
    public void PerfectAshlordSwingCanLeaveTheYard()
    {
        var park = _content.Parks["harbor-diamond"];
        var input = new AtBatInput(
            _content.Must("rio"), _content.Must("ashlord"), _content.Must("cinder"), [],
            "fastball", false, true, 0, false, true,
            _content.Bats["furnace-club"], 80, SprayAimDeg: 0, PitchInZone: true);
        var best = 0.0;
        for (var seed = 0; seed < 20; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(input, park, new Random(seed));
            if (r.HomeRun) best = Math.Max(best, r.CarryFt);
            best = Math.Max(best, r.CarryFt);
        }
        Assert.True(best > 280, $"best carry {best}");
    }

    [Fact]
    public void TrajectoryHasHangTime()
    {
        var samples = BallFlight.Trajectory(95, 28, 0);
        Assert.True(samples.Count > 10);
        Assert.InRange(BallFlight.HangTime(samples), 3.0, 6.5);
        var p = BallFlight.PointAt(samples, 0, 0.5);
        Assert.True(p.Y > 2);
        Assert.True(p.Z > 0);
    }

    [Fact]
    public void SparkStartsWithMoreStars()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.True(match.HomeStars >= match.AwayStars);
        Assert.Equal("Rio Sparks", match.Home.Captain.Name);
        Assert.Equal("Ashlord", match.Away.Captain.Name);
    }

    [Fact]
    public void CrystalRinkHasFreezeHazards()
    {
        var match = Match.Slice(_content, seed: 1, parkId: "crystal-rink");
        Assert.Equal("crystal-rink", match.Park.Id);
        Assert.Equal("ice", match.Park.Surface);
        Assert.True(ParkHazards.InFreeze(match.Park, 40, 70));
        Assert.False(ParkHazards.InFreeze(match.Park, 0, 0));
    }

    [Fact]
    public void SwapPitcherChangesTheMound()
    {
        var match = Match.Slice(_content, seed: 1);
        var first = match.Pitcher.Id;
        Assert.True(match.SwapPitcher());
        Assert.NotEqual(first, match.Pitcher.Id);
        Assert.True(match.PitcherStamina >= 35);
    }

    [Fact]
    public void CrystalGameFinishes()
    {
        var match = Match.Slice(_content, innings: 3, seed: 11, parkId: "crystal-rink");
        match.AutoPlayGame();
        Assert.True(match.Over);
        Assert.True(match.Log.Count > 8);
    }

    [Fact]
    public void FunfairHasWarpPipes()
    {
        var park = _content.Parks["funfair-park"];
        Assert.Equal("funfair-park", park.Id);
        Assert.Equal("grass", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "A");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "B");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "C");
        var w = ParkHazards.WarpIfPipe(park, 20, 55, new Random(3));
        Assert.True(w.Warped);
        Assert.False(Math.Abs(w.X - 20) < 0.01 && Math.Abs(w.Z - 55) < 0.01);
    }

    [Fact]
    public void RooftopHasBillboards()
    {
        var park = _content.Parks["rooftop-city"];
        Assert.Equal("rooftop-city", park.Id);
        Assert.Equal("dirt", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "billboard");
        Assert.Contains(park.Hazards, h => h.Type == "ac_unit");
        Assert.True(ParkHazards.HitStarSign(park, -80, 240));
        Assert.False(ParkHazards.HitStarSign(park, 0, 0));
    }

    [Fact]
    public void CycleBatChangesLoadout()
    {
        var match = Match.Slice(_content, seed: 1);
        var first = match.HomeBat.Id;
        match.CycleBat(true);
        Assert.NotEqual(first, match.HomeBat.Id);
        var g = match.HomeGlove.Id;
        match.CycleGlove(true);
        Assert.NotEqual(g, match.HomeGlove.Id);
    }

    [Fact]
    public void FourParksFinishAGame()
    {
        foreach (var id in new[] { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city" })
        {
            var match = Match.Slice(_content, innings: 3, seed: 5, parkId: id);
            match.AutoPlayGame();
            Assert.True(match.Over, id);
        }
    }

    [Fact]
    public void CanopyYardHasBarrelsAndClimbWalls()
    {
        var park = _content.Parks["canopy-yard"];
        Assert.Equal("canopy-yard", park.Id);
        Assert.Equal("dirt", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "barrel");
        Assert.Contains(park.Hazards, h => h.Type == "tree");
        Assert.Contains(park.Hazards, h => h.Type == "climb_wall");
        var w = ParkHazards.WarpIfPipe(park, 22, 58, new Random(3));
        Assert.True(w.Warped);
        Assert.Equal("barrel cannon", ParkHazards.WarpName(park));
        Assert.True(ParkHazards.CanClamber(park, _content.Must("konga")));
        Assert.False(ParkHazards.CanClamber(park, _content.Must("rio")));
    }

    [Fact]
    public void EmberKeepLavaSlowsFielders()
    {
        var park = _content.Parks["ember-keep"];
        Assert.Equal("ember-keep", park.Id);
        Assert.Equal("ash", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "lava_pit");
        Assert.Contains(park.Hazards, h => h.Type == "fire_breath");
        Assert.Contains(park.Hazards, h => h.Type == "statue");
        Assert.True(ParkHazards.InSlow(park, 38, 78));
        Assert.False(ParkHazards.InSlow(park, 0, 0));
        Assert.Equal("ember-keep", PresetTeams.HomeParkId("ashlord"));
        Assert.Equal("canopy-yard", PresetTeams.HomeParkId("konga"));
    }

    [Fact]
    public void ClamberRobsAJustOverFenceHomer()
    {
        var park = _content.Parks["canopy-yard"];
        var fence = AtBatResolver.FenceAt(park, 0);
        var hit = new AtBatResult(
            ContactQuality.Perfect, true, false, 102, 28, fence + 12, true, false, null, null);
        Assert.True(ParkHazards.CanClamberRob(park, _content.Must("konga"), hit));
        Assert.False(ParkHazards.CanClamberRob(park, _content.Must("ashlord"), hit));
        Assert.False(ParkHazards.CanClamberRob(_content.Parks["harbor-diamond"], _content.Must("konga"), hit));
    }

    [Fact]
    public void SixParksFinishAGame()
    {
        foreach (var id in new[]
                 { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" })
        {
            var match = Match.Exhibition(_content, "konga", "ashlord", innings: 3, seed: 8, parkId: id);
            match.AutoPlayGame();
            Assert.True(match.Over, id);
        }
    }
}
