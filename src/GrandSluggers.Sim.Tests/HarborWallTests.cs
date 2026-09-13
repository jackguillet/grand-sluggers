using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec D15 (#608): one number. The drawn padded wall (<see cref="HarborWall.Height"/>, what
/// <c>HarborKit.DressWall</c> builds) and the boundary the flight clips against
/// (<see cref="FieldBounds.Of"/>) read the same <see cref="Park.FenceHeightFt"/> on every segment.
/// </summary>
public sealed class HarborWallTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    public static TheoryData<string> Parks() => new() { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" };

    [Theory]
    [MemberData(nameof(Parks))]
    public void TheDrawnOutfieldTopIsTheParkFenceOnEveryLoopSegment(string id)
    {
        var park = _content.Parks[id];
        var n = HarborWall.Loop(park).Length;
        var outfield = 0;
        for (var i = 0; i < n; i++)
        {
            if (!HarborWall.IsOutfield(park, i)) continue;
            outfield++;
            Assert.Equal(park.FenceHeightFt, HarborWall.Height(park, i), 4);
        }
        Assert.True(outfield > HarborWall.OutfieldSegs / 2, $"{id}: {outfield} outfield vertices");
        Assert.True(HarborWall.OutfieldIsTheFence(park));

        // The flight's fair fence is the same top.
        var bounds = FieldBounds.Of(park);
        var fair = bounds.Segments.Where(s => s.Kind == FieldBounds.WallKind.FairFence).ToArray();
        Assert.NotEmpty(fair);
        Assert.All(fair, s => Assert.Equal(HarborWall.OutfieldHeight(park), s.HeightFt, 4));
        Assert.Equal(park.FenceHeightFt, bounds.FenceHeightFt);
    }

    [Theory]
    [MemberData(nameof(Parks))]
    public void TheFoulRailStaysHipHighAndMatchesTheFlightsFoulWall(string id)
    {
        var park = _content.Parks[id];
        Assert.Equal(4.2f, HarborWall.HipHeight);
        Assert.Equal(HarborWall.HipHeight, FieldBounds.FoulWallHeightFt, 4);
        var rail = 0;
        var n = HarborWall.Loop(park).Length;
        for (var i = 0; i < n; i++)
        {
            if (HarborWall.LoopPoint(park, i).Z > 95) continue;
            rail++;
            Assert.Equal(HarborWall.HipHeight, HarborWall.Height(park, i));
        }
        Assert.True(rail > 20, $"{id}: {rail} rail vertices");
        Assert.All(FieldBounds.Of(park).Segments.Where(s => s.Kind == FieldBounds.WallKind.FoulWall),
            s => Assert.Equal(FieldBounds.FoulWallHeightFt, s.HeightFt, 4));
        Assert.True(HarborWall.TaperIsARamp(park), $"{id}: the rail ramps up to the fence");
    }

    [Fact]
    public void HarborStandsAtTwelveFeet()
    {
        // The value is Jack's call (D15); twelve is the recommendation: taller than MLB's eight so a rob and a
        // carom read, far under the old 26-ft dressing so a deep fly is still a homer.
        Assert.Equal(12, _content.Parks["harbor-diamond"].FenceHeightFt);
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(12f)]
    [InlineData(26f)]
    public void AdsAndTheMarkSitInsideTheWallFace(float wallFt)
    {
        foreach (var want in new[] { HarborPostcard.AdHeightFt, 6.4f, 3.2f })
        {
            var (y, h) = HarborPostcard.OnWallFace(wallFt, want);
            Assert.True(h <= want);
            Assert.True(y + h * 0.5f <= wallFt - HarborPostcard.FaceMarginFt + 1e-4, $"{want} ft panel pokes over a {wallFt} ft cap");
            Assert.True(y - h * 0.5f >= HarborPostcard.FaceMarginFt - 1e-4, $"{want} ft panel sinks under the track");
        }
    }

    [Fact]
    public void AFenceUnderTheRailIsRefused()
    {
        var harbor = _content.Parks["harbor-diamond"];
        Assert.False(HarborWall.OutfieldIsTheFence(harbor with { FenceHeightFt = HarborWall.HipHeight }));
    }
}
