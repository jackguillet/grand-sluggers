using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ParkDiamondTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    Park Harbor => _content.Parks[HarborPostcard.ParkId];

    [Fact]
    public void SharedOutlinesAreNotAHarborHardcode()
    {
        Assert.True(ParkDiamond.PathIsNotALake());
        Assert.True(ParkDiamond.BagIsABag());
        Assert.True(ParkDiamond.HomePackedIsAPad());
        Assert.True(ParkDiamond.MoundIsAHill());
        Assert.True(ParkDiamond.StripeReadsAtCouch());
        Assert.True(ParkDiamond.StripesRunHomeToCf());
        Assert.True(ParkDiamond.PathCornersAreRound());
        Assert.True(ParkDiamond.BackApronIsCurved());
        Assert.True(ParkDiamond.DirtClearsTheLawn(),
            $"path top {ParkDiamond.PathTop:0.00} grass top {ParkDiamond.GrassTop:0.00} — dirt vanishes under the lawn");
        Assert.True(ParkDiamond.ChalkClearsTheDirt(),
            $"foul top {ParkDiamond.FoulY + ParkDiamond.FoulThick * 0.5f:0.00} dirt top {ParkDiamond.PathTop:0.00} — chalk is buried");
        Assert.True(ParkDiamond.LawnRespectsPits());
        Assert.True(ParkDiamond.OnDirt(0, 0), "home packed");
        Assert.True(ParkDiamond.OnDirt(Diamond.First.X, Diamond.First.Z), "1B pad");
        Assert.True(ParkDiamond.OnDirt(32, 32), "home-1B path");
        Assert.True(ParkDiamond.OnDirt(0, Diamond.Mound), "mound");
        Assert.False(ParkDiamond.OnDirt(0, 90), "inner grass Y");
        Assert.True(ParkDiamond.OnInfieldGrass(0, 90));
        Assert.False(ParkDiamond.OnDirt(0, 220), "outfield");
        Assert.True(ParkDiamond.OnDirt(0, Diamond.Second.Z + 16), "curved apron past 2B");
        Assert.False(ParkDiamond.OnDirt(43.3, 20.7), "foul of the thin home-1B path");
        var outer = ParkDiamond.OuterVerts();
        Assert.True(outer.Length > 16, "outer is a sampled loop, not 4 corners");
        var v1 = ParkDiamond.InnerVerts()[1];
        Assert.True(ParkDiamond.OnDirt(v1.X + ParkDiamond.BagPadR * 0.5, v1.Z), "1B pad");
    }

    [Fact]
    public void PolesAndTrackFollowTheParkFence()
    {
        Assert.True(ParkDiamond.PoleIsOnTheFoulLine(Harbor));
        Assert.True(ParkDiamond.PoleSitsOnThatParkFence(Harbor));
        Assert.True(ParkDiamond.TrackIsInsideTheWall(Harbor));
        Assert.True(ParkDiamond.TrackMid(Harbor, 0) < Harbor.CenterFenceFt);
        Assert.True(ParkDiamond.GrassZ1(Harbor) > FieldingResolver.InfieldLipFt);

        var shortPark = Harbor with { LeftFenceFt = 280, CenterFenceFt = 320, RightFenceFt = 280 };
        Assert.True(ParkDiamond.PoleIsOnTheFoulLine(shortPark));
        Assert.True(ParkDiamond.PoleSitsOnThatParkFence(shortPark));
        Assert.True(ParkDiamond.TrackIsInsideTheWall(shortPark));
        var harborPole = ParkDiamond.FoulPole(Harbor, 1);
        var shortPole = ParkDiamond.FoulPole(shortPark, 1);
        Assert.True(Diamond.Dist(0, 0, shortPole.X, shortPole.Z)
            < Diamond.Dist(0, 0, harborPole.X, harborPole.Z) - 20,
            "a shorter fence must pull the pole in — not a Harbor 330 hardcode");
        Assert.True(ParkDiamond.TrackMid(shortPark, 0) < ParkDiamond.TrackMid(Harbor, 0) - 20);
    }

    [Fact]
    public void InfieldDirtAuthoringMatchesParkDiamond()
    {
        var repo = Directory.GetParent(_content.Root)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var py = File.ReadAllText(Path.Combine(repo, "tools", "blender", "harbor_kit.py"));
        Assert.Contains("PATH_Y = 0.26", py);
        Assert.Contains("PATH_THICK = 0.24", py);
        Assert.Contains("MOUND_R = 9.2", py);
        Assert.Contains("MOUND_H = 0.98", py);
        Assert.DoesNotContain("MoundPad", py);
        Assert.DoesNotContain("MoundMid", py);
        Assert.Equal(0.26f, ParkDiamond.PathY);
        Assert.Equal(0.24f, ParkDiamond.PathThick);
        Assert.Equal(9.2f, ParkDiamond.MoundR);
        Assert.Equal(0.98f, ParkDiamond.MoundH);
    }

    [Fact]
    public void GrassHalfWidthClearsTheFoulLine()
    {
        Assert.True(ParkDiamond.GrassHalfWidth(200) > 200,
            "stripe must cover fair plus foul grass, not clip the line");
        Assert.True(ParkDiamond.GrassHalfWidth(0) >= 40);
    }
}
