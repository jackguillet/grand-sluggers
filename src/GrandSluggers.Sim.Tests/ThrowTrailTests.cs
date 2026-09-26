using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ThrowTrailTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));
    readonly ContentCatalog _content = Shipped.Content;

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
    public void SpecialFxNeverBuildsBallToFielderOrDestinationLines()
    {
        var path = Path.Combine(_repo, "unity/Assets/Scripts/Runtime/SpecialFx.cs");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("TrailRenderer", src, StringComparison.Ordinal);
        // A tell may draw its own line (a ribbon, a vine, a cable, a bolt, a ring: AB-C15), built in one place, keyed by the
        // tell. No line point is ever a glove or a throw's end: the special never draws a beam to a fielder or a destination.
        Assert.Equal(1, CountOf(src, "AddComponent<LineRenderer>"));
        var lines = File.ReadAllLines(path);
        foreach (var line in lines.Where(l => l.Contains("SetPosition(", StringComparison.Ordinal)))
            foreach (var banned in new[] { "Glove", "Throw", "Cover", "Bag" })
                Assert.DoesNotContain(banned, line, StringComparison.Ordinal);
        // Catalog throw slots remain metadata, never loaded as connector prefabs.
        foreach (var slot in ThrowTrail.Slots(_content.Art))
            Assert.DoesNotContain("\"" + slot.Id + "\"", src, StringComparison.Ordinal);
    }

    [Fact]
    public void InPlayThrowKeepsChemistryOnTheSphereAfterPlace()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/ActorDirector.cs"));
        var place = src.LastIndexOf("Park.Ball.Place(", StringComparison.Ordinal);
        var tint = src.LastIndexOf("SetTrailColor(SpecialFx.ThrowColor", StringComparison.Ordinal);
        Assert.True(place >= 0, "live ball still Places");
        Assert.True(tint >= 0, "throw chemistry still tints the sphere trail");
        Assert.True(tint > place, "Place's pitch color must not overwrite the throw trail");
    }

    static int CountOf(string text, string needle)
    {
        var n = 0;
        for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) n++;
        return n;
    }
}
