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
}
