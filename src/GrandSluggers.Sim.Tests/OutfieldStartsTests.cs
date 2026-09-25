using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Per-park foul territory and outfield starts (FD-07 C; F2-d; <c>SF-09</c>). A park that names no start stands each outfielder at
/// his bearing and his fraction of the fence the starts were authored against, on its own fence; a named start is used as
/// written; the infield never moves. A park that names no foul values plays <c>boundary.json</c>'s.
/// </summary>
public sealed class OutfieldStartsTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    static readonly string[] Outfield = ["LF", "CF", "RF"];

    /// <summary>Harbor's fence is the authored one, so its starts are the global starts to the bit; the infield is global everywhere.</summary>
    [Fact]
    public void SF09_HarborKeepsTheGlobalStartsAndNoParkMovesTheInfield()
    {
        var harbor = OutfieldStarts.Of(Catalog.MustPark(ParkId.Harbor), Catalog.Rules);
        foreach (var (pos, at) in Diamond.Positions) Assert.Equal(at, harbor[pos]);
        foreach (var park in Catalog.Parks.Values)
            foreach (var pos in Diamond.Positions.Keys.Except(Outfield))
                Assert.Equal(Diamond.Positions[pos], OutfieldStarts.Of(park, Catalog.Rules)[pos]);
    }

    /// <summary><c>SF-09</c>: elsewhere each outfielder keeps his bearing and his fraction of the fence, on the park's own fence.</summary>
    [Fact]
    public void SF09_ElsewhereEachOutfielderKeepsHisBearingAndHisFractionOfTheFence()
    {
        var a = Catalog.Rules.Fielders.AuthoredFence;
        foreach (var park in Catalog.Parks.Values)
        {
            var authored = park with { LeftFenceFt = (int)a.LeftFt, CenterFenceFt = (int)a.CenterFt, RightFenceFt = (int)a.RightFt, Fence = null };
            var starts = OutfieldStarts.Of(park, Catalog.Rules);
            foreach (var pos in Outfield)
            {
                var (gx, gz) = Diamond.Positions[pos];
                var (x, z) = starts[pos];
                var bearing = Math.Atan2(gx, gz) * 180 / Math.PI;
                Assert.Equal(bearing, Math.Atan2(x, z) * 180 / Math.PI, 9);
                var fraction = Math.Sqrt(gx * gx + gz * gz) / AtBatResolver.FenceAt(authored, bearing);
                Assert.Equal(fraction, Math.Sqrt(x * x + z * z) / AtBatResolver.FenceAt(park, bearing), 9);
            }
        }
    }

    /// <summary><c>SF-09</c>: a named start is used as written, and the others keep the default rule.</summary>
    [Fact]
    public void SF09_ANamedStartIsUsedAsWritten()
    {
        var park = Catalog.MustPark(ParkId.Rooftop) with { OutfieldStarts = new ParkOutfield(CF: new StartSpot(10, 200)) };
        var starts = OutfieldStarts.Of(park, Catalog.Rules);
        Assert.Equal((10.0, 200.0), starts["CF"]);
        Assert.Equal(OutfieldStarts.Of(Catalog.MustPark(ParkId.Rooftop), Catalog.Rules)["LF"], starts["LF"]);
    }

    /// <summary>A park that names foul values plays them over the table; one that names none plays the table's.</summary>
    [Fact]
    public void AParksFoulValuesSitOverTheTable()
    {
        var harbor = Catalog.MustPark(ParkId.Harbor);
        Assert.Equal(ParkBoundary.From(Rules.Default.Boundary), ParkBoundary.For(harbor, Rules.Default));
        var wide = harbor with { Foul = new ParkFoul(OffsetFt: 30) };
        var b = ParkBoundary.For(wide, Rules.Default);
        Assert.Equal(30, b.FoulOffsetFt);
        Assert.Equal(ParkBoundary.From(Rules.Default.Boundary).FlareStartFt, b.FlareStartFt);
        Assert.False(FieldBounds.Of(harbor, Rules.Default).Segments.SequenceEqual(FieldBounds.Of(wide, Rules.Default).Segments), "the wider wrap moves the rail");
        Assert.All(Catalog.Parks.Values, p => Assert.Null(p.Foul));
    }
}
