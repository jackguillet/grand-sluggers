using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The park-neutral boundary (#826, F2-a; spec §6.1, §16, Appendix B.9 <c>SF-08</c>). The foul rail, its
/// flare, the rail top, the backstop and the dugout pad left <see cref="HarborWall"/>'s literals for
/// <c>data/rules/boundary.json</c> at exactly the values that shipped, so the polygon the flight clips
/// against is the same polygon it was — vertex for vertex, on both data roots.
///
/// <para>
/// <see cref="SF08_TheBoundaryFromTheTableIsTodaysPolygon"/> is the parity gate, and it is deliberately
/// written as a transcription rather than as a golden file: <see cref="TodaysPolygon"/> is the shipped
/// <c>FieldBounds.Build</c> of <c>776c80a2</c> with the four numbers written out as the literals they
/// were (<c>36f</c>, <c>95</c>, <c>4.2f</c>, and the 0.7071067811865476 the rail projects on). It was
/// run green against that code before the extraction, so it is today's polygon and not a copy of the
/// new one. A transcription also reads the same on both roots, where a golden would need two copies.
/// </para>
/// </summary>
[Trait("Rows", "compact")]
public sealed class BoundaryTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    public static TheoryData<string> Parks() => new()
        { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" };

    [Theory]
    [MemberData(nameof(Parks))]
    public void SF08_TheBoundaryFromTheTableIsTodaysPolygon(string id)
    {
        var park = _content.Parks[id];
        var expected = TodaysPolygon(park);
        var actual = FieldBounds.Of(park).Segments;

        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];
            // Exact, not to a tolerance: the extraction moved a number's home, not its value.
            Assert.Equal(e, a);
        }
    }

    /// <summary>
    /// <c>FieldBounds.Build</c> as it shipped at <c>776c80a2</c>, with <see cref="HarborWall"/>'s
    /// constants written out. Nothing here may be "fixed": it is the before picture.
    /// </summary>
    static IReadOnlyList<FieldBounds.WallSegment> TodaysPolygon(Park park)
    {
        const int fenceSegs = 48;
        const int foulSegs = 20;
        const int homeSegs = 16;
        // HarborWall.HipHeight was a float const; (double)4.2f is 4.19999980926514, not 4.2.
        double foulTop = 4.2f;

        var pts = new List<(double X, double Z)>(fenceSegs + 2 * foulSegs + homeSegs + 4);
        for (var i = 0; i <= fenceSegs; i++)
        {
            var spray = -AtBatResolver.FoulLineDeg + 2 * AtBatResolver.FoulLineDeg * i / fenceSegs;
            pts.Add(BallFlight.GroundPoint(AtBatResolver.FenceAt(park, spray), spray));
        }
        var right = FoulSide(park, 1);
        pts.AddRange(right);
        var rightHome = right[^1];
        var r = Math.Sqrt(rightHome.X * rightHome.X + rightHome.Z * rightHome.Z);
        var a0 = Math.Atan2(rightHome.X, rightHome.Z);
        for (var i = 1; i < homeSegs; i++)
        {
            var a = a0 + (2 * Math.PI - 2 * a0) * i / homeSegs;
            pts.Add((Math.Sin(a) * r, Math.Cos(a) * r));
        }
        var left = FoulSide(park, -1);
        for (var i = left.Count - 1; i >= 0; i--)
            pts.Add(left[i]);

        var segs = new List<FieldBounds.WallSegment>(pts.Count);
        for (var i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            var fair = i < fenceSegs;
            var kind = fair ? FieldBounds.WallKind.FairFence : FieldBounds.WallKind.FoulWall;
            var height = fair ? park.FenceHeightFt : foulTop;
            var ex = b.X - a.X;
            var ez = b.Z - a.Z;
            var len = Math.Sqrt(ex * ex + ez * ez);
            if (len < 1e-6) continue;
            var nx = ez / len;
            var nz = -ex / len;
            var mx = (a.X + b.X) * 0.5;
            var mz = (a.Z + b.Z) * 0.5;
            if (nx * mx + nz * mz < 0)
            {
                nx = -nx;
                nz = -nz;
            }
            var ra = Math.Sqrt(a.X * a.X + a.Z * a.Z);
            var rb = Math.Sqrt(b.X * b.X + b.Z * b.Z);
            segs.Add(new FieldBounds.WallSegment(a.X, a.Z, b.X, b.Z, height, kind, nx, nz, Math.Min(ra, rb), Math.Max(ra, rb)));
        }
        return segs;
    }

    static List<(double X, double Z)> FoulSide(Park park, int sign)
    {
        var poleR = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
        var side = new List<(double X, double Z)>(22);
        for (var i = 1; i <= 20; i++)
            side.Add(Rail(sign, poleR * (1 - i / 20.0), poleR));
        return side;
    }

    /// <summary><c>HarborWall.FoulWall</c> as it shipped: offset 36 ft, flare from 95 ft.</summary>
    static (double X, double Z) Rail(int sign, double alongFt, double poleFt)
    {
        var inv = 0.7071067811865476;
        var s = Math.Max(0, alongFt);
        var flareStart = 95;
        var u = s <= flareStart || poleFt <= flareStart
            ? 1
            : 1 - Math.Clamp((s - flareStart) / (poleFt - flareStart), 0, 1);
        u = u * u * (3 - 2 * u);
        var off = 36f * u;
        return (sign * (inv * s + inv * off), inv * s - inv * off);
    }
}
