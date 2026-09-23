using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The park-neutral boundary (#826, F2-a; spec §6.1, §16, Appendix B.9 <c>SF-08</c>). The foul rail, its
/// flare, the rail top, the backstop and the dugout pad left <see cref="HarborWall"/>'s literals for
/// <c>data/rules/boundary.json</c> at exactly the values that shipped, so the polygon the flight clips
/// against is the same polygon it was — vertex for vertex.
///
/// <para>
/// <see cref="SF08_TheBoundaryFromTheTableIsTodaysPolygon"/> is the parity gate, and it is deliberately
/// written as a transcription rather than as a golden file: <see cref="TodaysPolygon"/> is the shipped
/// <c>FieldBounds.Build</c> of <c>776c80a2</c> with the four numbers written out as the literals they
/// were (<c>36f</c>, <c>95</c>, <c>4.2f</c>, and the 0.7071067811865476 the rail projects on). It was
/// run green against that code before the extraction, so it is today's polygon and not a copy of the
/// new one.
/// </para>
/// </summary>
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
    /// The table is the source of truth and it carries what shipped. The five numbers are pinned
    /// here as well as in the JSON because F2-a's whole claim is that it moved them without
    /// choosing them: #730 / #732 own the offset and the flare until they close.
    /// </summary>
    [Fact]
    public void TheTableCarriesTheNumbersThatShipped()
    {
        var shipped = Rules.Default.Boundary;
        Assert.Equal(36, shipped.FoulOffsetFt);
        Assert.Equal(95, shipped.FlareStartFt);
        Assert.Equal(4.2, shipped.RailHeightFt);
        Assert.Equal(-36, shipped.BackstopZFt);
        Assert.Equal(18, shipped.DugoutPadFt);

        // JSON = code fallback, for this table, by name. (RulesTests makes the same comparison over
        // every table; the row is here too because a boundary that drifts moves the polygon.)
        var defaults = new BoundaryRules();
        Assert.Equal(defaults.FoulOffsetFt, shipped.FoulOffsetFt);
        Assert.Equal(defaults.FlareStartFt, shipped.FlareStartFt);
        Assert.Equal(defaults.RailHeightFt, shipped.RailHeightFt);
        Assert.Equal(defaults.BackstopZFt, shipped.BackstopZFt);
        Assert.Equal(defaults.DugoutPadFt, shipped.DugoutPadFt);

        // The kit and the flight read one edge, not two copies of it.
        Assert.Equal(ParkBoundary.Default.FoulOffsetFt, HarborWall.FoulOffset);
        Assert.Equal(ParkBoundary.Default.BackstopZFt, HarborWall.HomeZ);
        Assert.Equal(ParkBoundary.Default.DugoutPadFt, HarborWall.DugoutPad);
        Assert.Equal((double)HarborWall.HipHeight, FieldBounds.FoulWallHeightFt);
        Assert.Equal(ParkBoundary.Default.BackstopZFt, FieldBounds.BackstopZ);
    }

    /// <summary>
    /// The rail the geometry stands on is the authored number narrowed through <c>float</c>, because
    /// the constant it replaced was a <c>float</c>. Pinned so that widening it is a deliberate act
    /// with a changed polygon behind it, not a quiet 2e-7 ft under a refactor (#826, #732).
    /// </summary>
    [Fact]
    public void TheRailTopIsTheShippedFloat()
    {
        Assert.Equal(4.2, ParkBoundary.Default.RailHeightFt);
        Assert.Equal((double)4.2f, ParkBoundary.Default.RailTopFt);
        Assert.NotEqual(4.2, ParkBoundary.Default.RailTopFt);
        Assert.Equal(ParkBoundary.Default.RailTopFt, FieldBounds.FoulWallHeightFt);
    }

    /// <summary>
    /// The cache serves a polygon per (park, edge). Before #826 the key was the three posts and the
    /// fence top, so a park given a different foul area (F2-d) would have been handed the polygon
    /// built for the first one — with no test failing, because the posts matched.
    /// </summary>
    [Fact]
    public void ADifferentEdgeIsADifferentPolygon()
    {
        var park = _content.Parks["harbor-diamond"];
        var shipped = ParkBoundary.Default;
        var wide = shipped with { FoulOffsetFt = shipped.FoulOffsetFt + 10 };

        Assert.Same(FieldBounds.Of(park), FieldBounds.Of(park, shipped));
        var other = FieldBounds.Of(park, wide);
        Assert.NotSame(FieldBounds.Of(park), other);
        Assert.Same(other, FieldBounds.Of(park, wide));

        // And it is a different field, not just a different object: the rail moved into foul.
        var near = FieldBounds.Of(park).RadiusAt(70);
        var wider = other.RadiusAt(70);
        Assert.True(wider > near + 1, $"the wider foul area should push the rail out: {near} -> {wider}");

        // A taller rail is a taller wall on the foul segments only.
        var tall = FieldBounds.Of(park, shipped with { RailHeightFt = 9 });
        Assert.All(tall.Segments.Where(s => s.Kind == FieldBounds.WallKind.FoulWall),
            s => Assert.Equal(9f, (float)s.HeightFt));
        Assert.All(tall.Segments.Where(s => s.Kind == FieldBounds.WallKind.FairFence),
            s => Assert.Equal(park.FenceHeightFt, s.HeightFt));
    }

    [Theory]
    [InlineData("foulOffsetFt", 0.0, "boundary.foulOffsetFt must be greater than 0")]
    [InlineData("foulOffsetFt", -36.0, "boundary.foulOffsetFt must be greater than 0")]
    [InlineData("flareStartFt", 0.0, "boundary.flareStartFt must be greater than 0")]
    [InlineData("railHeightFt", 0.0, "boundary.railHeightFt must be greater than 0")]
    [InlineData("railHeightFt", -4.2, "boundary.railHeightFt must be greater than 0")]
    [InlineData("dugoutPadFt", -1.0, "boundary.dugoutPadFt must be greater than 0")]
    [InlineData("backstopZFt", 0.0, "boundary.backstopZFt must be behind the plate (less than 0)")]
    [InlineData("backstopZFt", 36.0, "boundary.backstopZFt must be behind the plate (less than 0)")]
    public void AnImpossibleEdgeIsRefusedByName(string field, double value, string expected)
    {
        using var fixture = new RulesCopy();
        fixture.Change("boundary.json", json => json[field] = value);

        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal)
                                     && e.Contains(fixture.Path("boundary.json"), StringComparison.Ordinal));
    }

    [Fact]
    public void AMisspelledEdgeFieldIsAnErrorNotASilentFallback()
    {
        using var fixture = new RulesCopy();
        fixture.Change("boundary.json", json => json["foulOffsetFeet"] = 40);

        Assert.Contains(RulesTable.Validate(fixture.Root),
            e => e.Contains("boundary.foulOffsetFeet is not a rule this table owns", StringComparison.Ordinal));
    }

    /// <summary>A copy of the shipped rules folder one row may edit. Sibling of <c>RulesTests.RulesFixture</c>.</summary>
    sealed class RulesCopy : IDisposable
    {
        static readonly JsonDocumentOptions Comments = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public RulesCopy()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-boundary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(System.IO.Path.Combine(Root, RulesTable.Directory));
            var source = System.IO.Path.Combine(ContentCatalog.Load().Root.Shipped, RulesTable.Directory);
            foreach (var file in Directory.GetFiles(source, "*.json"))
                File.Copy(file, System.IO.Path.Combine(Root, RulesTable.Directory, System.IO.Path.GetFileName(file)));
        }

        public string Root { get; }

        public string Path(string file) => System.IO.Path.Combine(Root, RulesTable.Directory, file);

        public void Change(string file, Action<JsonObject> change)
        {
            var json = JsonNode.Parse(File.ReadAllText(Path(file)), null, Comments)!.AsObject();
            change(json);
            File.WriteAllText(Path(file), json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
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
