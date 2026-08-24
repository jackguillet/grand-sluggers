using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StillRequestTests
{
    [Fact]
    public void DefaultRequestIsTitlePlateMoundHudOffRio()
    {
        var req = StillRequest.Parse("{}");
        Assert.Equal(new[] { "title", "plate", "mound" }, req.ResolvedShots());
        Assert.Equal("rio", req.ResolvedHome());
        Assert.Equal("ashlord", req.ResolvedAway());
        Assert.True(req.HudOff);
        Assert.False(req.FeelDebug);
        Assert.Equal(1920, req.ResolvedWidth());
        Assert.Equal(1080, req.ResolvedHeight());
        Assert.Equal("plate", AtBatShots.Plate);
        Assert.Equal("mound", AtBatShots.Mound);
        Assert.Contains("plate", StillRequest.AllowedShots);
        Assert.Contains("mound", StillRequest.AllowedShots);
        Assert.Contains("diamond-grounder", StillRequest.AllowedShots);
    }

    [Fact]
    public void ParseHonorsShotsHomeAwayAndRejectsUnknown()
    {
        var req = StillRequest.Parse("""
            {"shots":["plate","mound"],"home":"vale","away":"konga","hudOff":false,"width":1280,"height":720}
            """);
        Assert.Equal(new[] { "plate", "mound" }, req.ResolvedShots());
        Assert.Equal("vale", req.ResolvedHome());
        Assert.Equal("konga", req.ResolvedAway());
        Assert.False(req.HudOff);
        Assert.Equal(1280, req.ResolvedWidth());
        Assert.Equal(720, req.ResolvedHeight());
        Assert.Equal("/tmp/gs/plate.png", StillRequest.PngPath("/tmp/gs", "plate"));
        var ex = Assert.Throws<InvalidDataException>(() =>
            StillRequest.Parse("""{"shots":["catcher-spine"]}"""));
        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AwayWillNotMatchHome()
    {
        var req = StillRequest.Parse("""{"home":"rio","away":"rio"}""");
        Assert.Equal("rio", req.ResolvedHome());
        Assert.NotEqual("rio", req.ResolvedAway());
    }
}
