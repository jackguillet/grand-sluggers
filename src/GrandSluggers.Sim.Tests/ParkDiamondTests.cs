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
        Assert.True(ParkDiamond.DirtClearsTheLawn(),
            $"path top {ParkDiamond.PathTop:0.00} grass top {ParkDiamond.GrassTop:0.00} — dirt vanishes under the lawn");
        Assert.True(ParkDiamond.LawnRespectsPits());
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
        Assert.Equal(0.26f, ParkDiamond.PathY);
        Assert.Equal(0.24f, ParkDiamond.PathThick);
    }

    [Fact]
    public void GrassHalfWidthClearsTheFoulLine()
    {
        Assert.True(ParkDiamond.GrassHalfWidth(200) > 200,
            "stripe must cover fair plus foul grass, not clip the line");
        Assert.True(ParkDiamond.GrassHalfWidth(0) >= 40);
    }
}
