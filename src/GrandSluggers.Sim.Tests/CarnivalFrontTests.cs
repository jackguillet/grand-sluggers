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
        Assert.True(CarnivalFront.LogoZ > CarnivalFront.FeaturedTitleZ,
            $"logo on the toy z={CarnivalFront.LogoZ} hero={CarnivalFront.FeaturedTitleZ}");
        Assert.InRange(CarnivalFront.LogoZ, -10, 16);
        Assert.True(Math.Abs(CarnivalFront.LogoX) < 8, $"logo off-frame x={CarnivalFront.LogoX}");
        Assert.True(CarnivalFront.LogoY > 10, $"logo through the hat y={CarnivalFront.LogoY}");
        Assert.Equal("GRAND SLUGGERS", CarnivalFront.Logo);
    }

    [Fact]
    public void TitleIsOneToyAndAStickerOverTheInfield()
    {
        var title = ContentCatalog.Load().Shots.Must("title");
        Assert.True(CarnivalFront.TitlePoster(title.Pos, title.Target),
            $"title is not a sticker poster cam={title.Pos} look={title.Target} " +
            $"heroDeg={CarnivalFront.OffLook(title.Pos, title.Target, CarnivalFront.TitleHeroChest):0.0} " +
            $"logoDeg={CarnivalFront.OffLook(title.Pos, title.Target, CarnivalFront.TitleLogoAt):0.0} " +
            $"sep={CarnivalFront.OffLook(title.Pos, CarnivalFront.TitleHeroChest, CarnivalFront.TitleLogoAt):0.0}");
        var row = CarnivalFront.CaptainSpot(1, 6, select: false, home: false);
        Assert.True(row.Z > CarnivalFront.FeaturedTitleZ + 8,
            $"title row should wait off-frame z={row.Z}");
    }

    [Fact]
    public void SelectLookIsTheChestNotTheBrim()
    {
        var look = CarnivalFront.SelectLook(5, 6);
        Assert.Equal(0f, look.X);
        Assert.Equal(CarnivalFront.FeaturedSelectZ, look.Z);
        Assert.True(look.Y < 3.2f, $"select look is the brim y={look.Y}");
        Assert.True(CarnivalFront.FeaturedSelectZ >= 6.5f, $"select pick too close z={CarnivalFront.FeaturedSelectZ}");
        Assert.True(CarnivalFront.FeaturedSelectZ < CarnivalFront.SelectRowZ);
        Assert.Equal(CarnivalFront.SelectRowZ - CarnivalFront.FeaturedSelectZ, CarnivalFront.HomeStepSelectFt);
    }

    [Fact]
    public void SelectCamIsTheToyNotTheBerm()
    {
        Assert.True(CarnivalFront.SelectCamIsTheToy(4.6, -10));
        Assert.False(CarnivalFront.SelectCamIsTheToy(7.8, -12), "y=7.8 at z=-12 looks down at the plate dirt");
        Assert.False(CarnivalFront.SelectCamIsTheToy(4.4, 4), "z=4 / look y=4.4 is Ashlord's brim");
        Assert.False(CarnivalFront.SelectCamIsTheToy(2.0, -10), "cam in the dirt");
        Assert.False(CarnivalFront.SelectCamIsTheToy(4.6, -22), "through the backstop cage");
    }
}
