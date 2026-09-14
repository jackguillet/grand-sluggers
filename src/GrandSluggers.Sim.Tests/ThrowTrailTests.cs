using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ThrowTrailTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root, ".."));
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void CatalogThrowVfxNeverDrawADestinationLine()
    {
        Assert.False(ThrowTrail.DestinationLine);
        var slots = ThrowTrail.Slots(_content.Art);
        Assert.Contains(slots, s => s.Id.Equals("throw-trail-good", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(slots, s => s.Id.Equals("throw-trail-bad", StringComparison.OrdinalIgnoreCase));
        Assert.True(slots.Count >= 2, "keep the throw-trail catalog slots");
        foreach (var vfx in _content.Art.Vfx)
        {
            if (!ThrowTrail.IsThrow(vfx)) continue;
            Assert.False(ThrowTrail.DrawsDestinationLine(vfx), vfx.Id);
            Assert.Equal(0, ThrowTrail.DestinationPositions(vfx));
        }
        foreach (var vfx in slots)
        {
            Assert.True(ThrowTrail.IsThrow(vfx), vfx.Id);
            Assert.False(ThrowTrail.DrawsDestinationLine(vfx), vfx.Id);
            Assert.Equal(0, ThrowTrail.DestinationPositions(vfx));
        }
    }

    [Fact]
    public void ChemistryTintsTheBallNotALineThroughTheDirt()
    {
        var good = ThrowTrail.BallRgb(Chemistry.Good);
        var bad = ThrowTrail.BallRgb(Chemistry.Bad);
        var juice = CartoonJuice.ThrowRgb(Chemistry.Good);
        Assert.Equal(juice, good);
        Assert.True(good.B > good.G, "good chem is a purple ball trail");
        Assert.True(bad.R > bad.B && bad.G >= bad.B * 0.5, "bad chem is muddy");
    }

    [Fact]
    public void SpecialFxHonorsThrowTrailForEveryCatalogThrowSlot()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/SpecialFx.cs"));
        Assert.Contains("ThrowTrail.Slots", src, StringComparison.Ordinal);
        Assert.Contains("ThrowTrail.DrawsDestinationLine", src, StringComparison.Ordinal);
        Assert.Contains("ThrowTrail.DestinationPositions", src, StringComparison.Ordinal);
        Assert.DoesNotContain("_throw.enabled = on", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Vector3.Lerp(_throwFrom, _throwTo", src, StringComparison.Ordinal);
    }

    [Fact]
    public void InPlayThrowKeepsChemistryOnTheSphereAfterPlace()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/ActorDirector.cs"));
        var place = src.LastIndexOf("_park.Ball.Place(", StringComparison.Ordinal);
        var tint = src.LastIndexOf("SetTrailColor(SpecialFx.ThrowColor", StringComparison.Ordinal);
        Assert.True(place >= 0, "live ball still Places");
        Assert.True(tint >= 0, "throw chemistry still tints the sphere trail");
        Assert.True(tint > place, "Place's pitch color must not overwrite the throw trail");
    }
}
