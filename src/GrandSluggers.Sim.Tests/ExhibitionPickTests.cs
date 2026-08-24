using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ExhibitionPickTests
{
    [Fact]
    public void CyclingTheCaptainDoesNotMoveTheField()
    {
        var start = new ExhibitionPick("rio", "ashlord", "harbor-diamond");
        var next = ExhibitionPick.CycleHome(start, 1);
        Assert.Equal("vale", next.Home);
        Assert.Equal("ashlord", next.Away);
        Assert.Equal("harbor-diamond", next.Park);
        Assert.Equal("crystal-rink", PresetTeams.HomeParkId("vale"));
        Assert.NotEqual(PresetTeams.HomeParkId(next.Home), next.Park);

        var prev = ExhibitionPick.CycleHome(start, -1);
        Assert.Equal("ashlord", prev.Home);
        Assert.NotEqual("ashlord", prev.Away);
        Assert.Equal("harbor-diamond", prev.Park);
    }

    [Fact]
    public void CyclingTheFieldDoesNotMoveTheCaptains()
    {
        var start = new ExhibitionPick("rio", "ashlord", "harbor-diamond");
        var next = ExhibitionPick.CyclePark(start, 1);
        Assert.Equal("rio", next.Home);
        Assert.Equal("ashlord", next.Away);
        Assert.Equal("crystal-rink", next.Park);

        var wrap = ExhibitionPick.CyclePark(start, -1);
        Assert.Equal("ember-keep", wrap.Park);
        Assert.Equal("rio", wrap.Home);
    }

    [Fact]
    public void AwaySkipWhenItWouldMatchHome()
    {
        var pick = new ExhibitionPick("rio", "vale", "harbor-diamond");
        var next = ExhibitionPick.CycleAway(pick, -1);
        Assert.Equal("rio", next.Home);
        Assert.NotEqual("rio", next.Away);
        Assert.Equal("harbor-diamond", next.Park);
    }
}
