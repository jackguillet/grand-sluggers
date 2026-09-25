using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The park-neutral bowl of bleachers (<see cref="KitBowl"/>): built from each park's own wall loop, it stands outside the
/// field everywhere, climbs away from it, leaves centre field open, and its corner banks start under the fence's top.
/// </summary>
public sealed class KitBowlTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;

    public static TheoryData<string> Parks() => [.. ParkId.All];

    [Theory]
    [MemberData(nameof(Parks))]
    public void EveryParkHasAHorseshoeAndTwoCornerBanksOfSevenRows(string id)
    {
        var bowl = KitBowl.Of(Catalog.Parks[id], Rules.Default);
        Assert.Equal([KitBowl.CornerRight, KitBowl.Horseshoe, KitBowl.CornerLeft], bowl.Select(p => p.Kind));
        Assert.All(bowl, p => Assert.Equal(KitBowl.Rows, p.Treads.Count));
    }

    /// <summary>Every tread, front and back edge, stands outside the wall loop by at least its piece's gap.</summary>
    [Theory]
    [MemberData(nameof(Parks))]
    public void EveryTreadStandsOutsideTheWall(string id)
    {
        var park = Catalog.Parks[id];
        var loop = HarborWall.Loop(park, Rules.Default);
        foreach (var piece in KitBowl.Of(park, Rules.Default))
        {
            var gap = piece.Kind == KitBowl.Horseshoe ? KitBowl.FoulGapFt : KitBowl.WallGapFt;
            foreach (var t in piece.Treads)
                for (var j = 0; j < t.Front.Count; j++)
                {
                    foreach (var p in new[] { t.Front[j], (t.Front[j].X + t.Out[j].X * HarborStands.RowRun, t.Front[j].Z + t.Out[j].Z * HarborStands.RowRun) })
                    {
                        Assert.False(Inside(loop, p), $"{id} {piece.Kind} row {t.Row} point {j} ({p.Item1:F1}, {p.Item2:F1}) is inside the wall");
                        Assert.True(Distance(loop, p) >= gap - 0.75, $"{id} {piece.Kind} row {t.Row} point {j} is {Distance(loop, p):F2} ft from the wall");
                    }
                }
        }
    }

    /// <summary>Each row is higher than the one in front of it and farther from the wall; no bank stands in centre field.</summary>
    [Theory]
    [MemberData(nameof(Parks))]
    public void RowsClimbAwayFromTheFieldAndCenterFieldIsOpen(string id)
    {
        var park = Catalog.Parks[id];
        var loop = HarborWall.Loop(park, Rules.Default);
        foreach (var piece in KitBowl.Of(park, Rules.Default))
        {
            for (var r = 1; r < piece.Treads.Count; r++)
            {
                Assert.True(piece.Treads[r].Y > piece.Treads[r - 1].Y);
                for (var j = 0; j < piece.Treads[r].Front.Count; j++)
                    Assert.True(Distance(loop, piece.Treads[r].Front[j]) > Distance(loop, piece.Treads[r - 1].Front[j]) + 1.5);
            }
            foreach (var p in piece.Treads[0].Front)
                Assert.True(Math.Abs(KitBowl.SprayDeg(p)) >= KitBowl.OpenSprayDeg - 1, $"{id} {piece.Kind} stands in centre field");
        }
    }

    /// <summary>A corner bank starts under the top of the fence it stands behind; the horseshoe starts low, at the rail.</summary>
    [Theory]
    [MemberData(nameof(Parks))]
    public void CornerBanksStartUnderTheCapAndTheHorseshoeAtTheRail(string id)
    {
        var park = Catalog.Parks[id];
        foreach (var piece in KitBowl.Of(park, Rules.Default))
        {
            var y0 = piece.Treads[0].Y;
            if (piece.Kind == KitBowl.Horseshoe)
                Assert.True(y0 < HarborWall.HipHeight(Rules.Default), $"{id} horseshoe starts at {y0}");
            else
                Assert.True(y0 < park.FenceHeightFt + 4 && y0 > HarborWall.HipHeight(Rules.Default), $"{id} {piece.Kind} starts at {y0}");
        }
    }

    [Fact]
    public void SectionsAreSplitByAisles()
    {
        Assert.Equal(0, KitBowl.SectionAt(0));
        Assert.Equal(0, KitBowl.SectionAt(KitBowl.SectionFt - 0.1));
        Assert.Equal(-1, KitBowl.SectionAt(KitBowl.SectionFt + 0.1));
        Assert.Equal(1, KitBowl.SectionAt(KitBowl.SectionFt + KitBowl.AisleFt + 0.1));
    }

    static bool Inside((double X, double Z)[] poly, (double X, double Z) p)
    {
        var inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            var a = poly[i];
            var b = poly[j];
            if ((a.Z > p.Z) != (b.Z > p.Z) && p.X < (b.X - a.X) * (p.Z - a.Z) / (b.Z - a.Z) + a.X)
                inside = !inside;
        }
        return inside;
    }

    static double Distance((double X, double Z)[] poly, (double X, double Z) p)
    {
        var best = double.MaxValue;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            var a = poly[j];
            var b = poly[i];
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            var len2 = dx * dx + dz * dz;
            var t = len2 < 1e-12 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Z - a.Z) * dz) / len2, 0, 1);
            var ex = a.X + dx * t - p.X;
            var ez = a.Z + dz * t - p.Z;
            best = Math.Min(best, Math.Sqrt(ex * ex + ez * ez));
        }
        return best;
    }
}
