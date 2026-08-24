using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StillHarnessTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void SkipToHomeHalfMakesHomeBatWithoutPlayingTheTop()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        Assert.True(match.Top);
        var awayBatter = match.Batter.Id;
        match.SkipToHomeCaptainAtBat();
        Assert.False(match.Top);
        Assert.Equal(0, match.Outs);
        Assert.Equal(0, match.Balls);
        Assert.Equal(0, match.Strikes);
        Assert.False(match.Over);
        Assert.NotEqual(awayBatter, match.Batter.Id);
        Assert.Equal("rio", match.Home.Captain.Id);
        Assert.Equal("rio", match.Batter.Id);
    }

    [Fact]
    public void GiveOffenseStarsUnlocksStarSwingOnTheHomeHalf()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        match.SkipToHomeCaptainAtBat();
        match.GiveOffenseStars(5);
        Assert.True(match.CanStarSwing);
        Assert.False(string.IsNullOrWhiteSpace(match.Batter.StarSwing));
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        Assert.Equal("diamond-grounder", InPlay.TheaterShot(hopper));
        var star = hopper with { StarSwingUsed = match.Batter.StarSwing, LaunchDeg = 28 };
        Assert.Equal("smash", InPlay.TheaterShot(star));
    }
}
