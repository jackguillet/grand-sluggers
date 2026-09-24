using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ExhibitionPickTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void CyclingTheCaptainDoesNotMoveTheField()
    {
        var start = new ExhibitionPick("rio", "ashlord", "harbor-diamond");
        var next = ExhibitionPick.CycleHome(Shipped.Content, start, 1);
        Assert.Equal("vale", next.Home);
        Assert.Equal("ashlord", next.Away);
        Assert.Equal("harbor-diamond", next.Park);
        Assert.Equal("crystal-rink", PresetTeams.HomeParkId(_content, "vale"));
        Assert.NotEqual(PresetTeams.HomeParkId(_content, next.Home), next.Park);

        var prev = ExhibitionPick.CycleHome(Shipped.Content, start, -1);
        Assert.Equal("fenn", prev.Home);
        Assert.NotEqual("fenn", prev.Away);
        Assert.Equal("harbor-diamond", prev.Park);
    }

    [Fact]
    public void CyclingTheFieldDoesNotMoveTheCaptains()
    {
        var start = new ExhibitionPick("rio", "ashlord", "harbor-diamond");
        var next = ExhibitionPick.CyclePark(_content, start, 1);
        Assert.Equal("rio", next.Home);
        Assert.Equal("ashlord", next.Away);
        Assert.Equal("crystal-rink", next.Park);

        var wrap = ExhibitionPick.CyclePark(_content, start, -1);
        Assert.Equal("ember-keep", wrap.Park);
        Assert.Equal("rio", wrap.Home);
    }

    [Fact]
    public void AwaySkipWhenItWouldMatchHome()
    {
        var pick = new ExhibitionPick("rio", "vale", "harbor-diamond");
        var next = ExhibitionPick.CycleAway(Shipped.Content, pick, -1);
        Assert.Equal("rio", next.Home);
        Assert.NotEqual("rio", next.Away);
        Assert.Equal("harbor-diamond", next.Park);
    }

    [Fact]
    public void Pad1CanSitAwayWithoutMovingCaptainsOrPark()
    {
        var start = ExhibitionPick.Default;
        Assert.True(start.Pad1Home);
        Assert.Equal("rio", start.Yours);
        Assert.Equal("ashlord", start.Theirs);
        var away = ExhibitionPick.ToggleSeat(start);
        Assert.False(away.Pad1Home);
        Assert.Equal("rio", away.Home);
        Assert.Equal("ashlord", away.Away);
        Assert.Equal("ashlord", away.Yours);
        Assert.Equal("rio", away.Theirs);
        Assert.Equal("harbor-diamond", away.Park);
        Assert.True(ExhibitionPick.ToggleSeat(away).Pad1Home);
    }

    [Fact]
    public void CycleYoursFollowsTheSeat()
    {
        var home = ExhibitionPick.Default;
        var nextHome = ExhibitionPick.CycleYours(Shipped.Content, home, 1);
        Assert.Equal("vale", nextHome.Home);
        Assert.Equal("ashlord", nextHome.Away);
        var away = ExhibitionPick.ToggleSeat(home);
        var nextAway = ExhibitionPick.CycleYours(Shipped.Content, away, 1);
        Assert.Equal("rio", nextAway.Home);
        Assert.NotEqual("ashlord", nextAway.Away);
        Assert.Equal(nextAway.Away, nextAway.Yours);
    }
}
