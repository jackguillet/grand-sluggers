using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CarnivalFrontTests
{
    [Fact]
    public void HarborIsTheRealDiamondPostcard()
    {
        Assert.Equal("GRAND SLUGGERS", CarnivalFront.Logo);
        Assert.Contains("play ball", CarnivalFront.PlayBall, StringComparison.OrdinalIgnoreCase);
        Assert.True(CarnivalFront.HarborIsTheProduct("harbor-diamond"));
        Assert.False(CarnivalFront.HarborIsTheProduct("crystal-rink"));
        Assert.Contains("real diamond", CarnivalFront.Gimmick("harbor-diamond", false), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fireworks", CarnivalFront.Gimmick("harbor-diamond", true), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DAY", CarnivalFront.SkyGag(false));
        Assert.Equal("NIGHT", CarnivalFront.SkyGag(true));
    }

    [Fact]
    public void CrystalAndFunfairGimmicksChangeAtNight()
    {
        Assert.Contains("Ice", CarnivalFront.Gimmick("crystal-rink", false));
        Assert.Contains("lights", CarnivalFront.Gimmick("crystal-rink", true), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Pipes", CarnivalFront.Gimmick("funfair-park", false));
        Assert.Contains("Chompers", CarnivalFront.Gimmick("funfair-park", true));
    }

    [Fact]
    public void HighlightedCaptainStepsTowardTheCamera()
    {
        var row = CarnivalFront.CaptainSpot(2, 6, select: true, home: false);
        var home = CarnivalFront.CaptainSpot(2, 6, select: true, home: true);
        Assert.True(home.Z < row.Z, $"home {home.Z} should be closer to camera than row {row.Z}");
        Assert.Equal(0f, home.X);
        Assert.NotEqual(row.X, home.X);
        var titleHome = CarnivalFront.CaptainSpot(0, 6, select: false, home: true);
        Assert.Equal(0f, titleHome.X);
        Assert.True(titleHome.Z < CarnivalFront.TitleRowZ);
        Assert.True(CarnivalFront.HomeStepSelectFt > CarnivalFront.HomeStepTitleFt);
        Assert.True(CarnivalFront.CardX > 4);
        Assert.True(CarnivalFront.CardY > 2);
        Assert.True(titleHome.Z < 16, $"title captain too far z={titleHome.Z}");
        Assert.InRange(CarnivalFront.LogoZ, -10, 16);
        Assert.True(Math.Abs(CarnivalFront.LogoX) < 8, $"logo off-frame x={CarnivalFront.LogoX}");
        Assert.True(CarnivalFront.LogoY > 5, $"logo y={CarnivalFront.LogoY}");
        Assert.Equal("GRAND SLUGGERS", CarnivalFront.Logo);
    }
}
