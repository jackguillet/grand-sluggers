using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec D15 (#608): one number. The drawn padded wall (<see cref="HarborWall.Height"/>, what
/// <c>FieldKit.Wall</c> builds at every park since #859) and the boundary the flight clips against
/// (<see cref="FieldBounds.Of"/>) read the same <see cref="Park.FenceHeightFt"/> on every segment.
///
/// <para>
/// D15 as amended by D21 (FD-06) is wider than one number: <b>the drawn wall equals the flight wall
/// on every span</b> (Appendix B.9 <c>SF-05</c>). #845 made that true of the shape as well as of the
/// top — the loop walks the park's own fence on both sides instead of mirroring right field onto
/// left, so Funfair (315 / 340), Rooftop (318 / 322) and Canopy (312 / 318) draw the wall a ball hit
/// to left actually meets.
/// </para>
/// </summary>
[Trait("Rows", "compact")]
public sealed class HarborWallTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    /// <summary>
    /// The catalog's own pick cycle (#820), not a literal, so a park added to <c>data/parks/</c> is
    /// covered the day it lands. <c>ParkSchemaTests</c> pins the same order on both roots.
    /// </summary>
    public static TheoryData<string> Parks()
    {
        var parks = new TheoryData<string>();
        foreach (var id in ContentCatalog.Load().ParkPickOrder) parks.Add(id);
        return parks;
    }

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

    // A gap on the C80 copy, reported and not repaired (#715): the rail's taper starts at a literal 95 ft in HarborWall (hipZ,
    // flareStart), which no overlay can move. On the copy's 0.70 lines the ramp is shorter, and the two 8-ft parks (funfair-park,
    // crystal-rink) get 4 taper vertices where TaperIsARamp asks for 6. The copy's run skips Copy=gap rows until promotion decides.
    [Theory]
    [Trait("Copy", "gap")]
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
            if (HarborWall.LoopPoint(park, i).Z > HarborWall.RampStartZ) continue;
            rail++;
            Assert.Equal(HarborWall.HipHeight, HarborWall.Height(park, i));
        }
        Assert.True(rail > 20, $"{id}: {rail} rail vertices");
        Assert.All(FieldBounds.Of(park).Segments.Where(s => s.Kind == FieldBounds.WallKind.FoulWall),
            s => Assert.Equal(FieldBounds.FoulWallHeightFt, s.HeightFt, 4));
        Assert.True(HarborWall.TaperIsARamp(park), $"{id}: the rail ramps up to the fence");
    }

    /// <summary>
    /// Feet. Every drawn vertex is built from the same <see cref="AtBatResolver.FenceAt"/> sample or
    /// the same <see cref="ParkBoundary.RailPoint"/> the polygon is built from, so the gap is
    /// rounding and nothing else: the worst measured on either root is 5e-15 ft. A tolerance this
    /// tight is the point — "near the wall" is what the mirror looked like from center field.
    /// </summary>
    const double OnTheWallFt = 1e-9;

    /// <summary>
    /// <c>SF-05</c> (D15 as amended by D21, FD-06; #845). The wall the player sees is the wall the
    /// ball meets, on every span of every park in the pick cycle, on both roots:
    ///
    /// <list type="bullet">
    /// <item>every drawn vertex lies on <see cref="FieldBounds.Of(Park)"/>'s polygon;</item>
    /// <item>a vertex on a fair span is drawn at the flight's fence top (D15);</item>
    /// <item>a vertex on a foul span is drawn at the flight's rail top, up to
    /// <see cref="HarborWall.RampStartZ"/>;</item>
    /// <item>each pole is drawn at <i>that side's</i> fence distance — which is the mirror this
    /// child removed: before #845 a lopsided park drew its right-field pole in left field.</item>
    /// </list>
    ///
    /// <para>
    /// Past <see cref="HarborWall.RampStartZ"/> the drawn rail ramps up to the fence while the
    /// flight's rail stays hip-high to the pole. The positions still agree there — only the tops
    /// disagree, and which of the two is the rule is §5 <b>Q5</b> (#732) and is Jack's. This row
    /// stops at the ramp rather than deciding it.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Parks))]
    public void SF05_TheDrawnWallIsTheFlightWallOnEverySpan(string id)
    {
        var park = _content.Parks[id];
        var bounds = FieldBounds.Of(park);
        var loop = HarborWall.Loop(park);
        Assert.Equal(HarborWall.WrapSegs, loop.Length);

        var fair = 0;
        var rail = 0;
        var ramped = 0;
        for (var i = 0; i < loop.Length; i++)
        {
            var p = loop[i];
            var (dist, kind) = NearestSpan(bounds, p.X, p.Z);
            Assert.True(dist <= OnTheWallFt,
                $"{id}: drawn vertex {i} at ({p.X:0.###}, {p.Z:0.###}) is {dist:0.###} ft off the flight wall");

            if (kind == FieldBounds.WallKind.FairFence)
            {
                fair++;
                Assert.True(HarborWall.IsOutfield(park, i), $"{id}: vertex {i} is on a fair span but is not drawn as outfield");
                Assert.Equal(bounds.FenceHeightFt, HarborWall.Height(park, i), 4);
            }
            else if (p.Z <= HarborWall.RampStartZ)
            {
                rail++;
                Assert.Equal(FieldBounds.FoulWallHeightFt, HarborWall.Height(park, i), 4);
            }
            else
            {
                ramped++;
            }
        }
        Assert.Equal(HarborWall.OutfieldSegs + 1, fair);
        Assert.True(ramped > 0, $"{id}: the ramp should cover the last feet of rail on each side");
        Assert.True(rail > ramped,
            $"{id}: {ramped} of {rail + ramped} foul vertices are past the ramp — this row would be checking the tail, not the rail");
        // The rail the kit draws through the infield stands off the line at the edge's own offset.
        Assert.Equal(ParkBoundary.Default.FoulOffsetFt, RailOffsetFt(loop), 6);

        // Each pole at its own distance. The mirror drew the right-field pole on both sides.
        foreach (var sign in new[] { -1, 1 })
        {
            var want = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
            var pole = loop.MinBy(p => Math.Abs(FieldBounds.SprayDeg(p.X, p.Z) - sign * AtBatResolver.FoulLineDeg));
            Assert.Equal(want, FieldBounds.DistHome(pole.X, pole.Z), 9);
        }
        Assert.Equal(park.LeftFenceFt == park.RightFenceFt, HarborWall.ParkIsSymmetric(park));
    }

    /// <summary>
    /// <c>SF-05</c> on a park the catalog does not carry: a fixture pulled 30 ft in on one line and
    /// 30 ft out on the other, so the row proves the builder and not today's data. On the shipped
    /// root that is 300 / 400 / 360, and before #845 its drawn left field stood at 360 ft — the
    /// right-field wall, mirrored — with every gate above it green.
    /// </summary>
    [Fact]
    public void SF05_ALopsidedParkDrawsItsOwnLeftFieldWall()
    {
        var harbor = _content.Parks[HarborPostcard.ParkId];
        var lopsided = harbor with
        {
            Id = "sf05-lopsided",
            LeftFenceFt = harbor.LeftFenceFt - 30,
            RightFenceFt = harbor.RightFenceFt + 30
        };

        var loop = HarborWall.Loop(lopsided);
        Assert.Equal(HarborWall.WrapSegs, loop.Length);
        var bounds = FieldBounds.Of(lopsided);
        for (var i = 0; i < loop.Length; i++)
        {
            var (dist, _) = NearestSpan(bounds, loop[i].X, loop[i].Z);
            Assert.True(dist <= OnTheWallFt, $"drawn vertex {i} is {dist:0.###} ft off the flight wall");
        }

        var left = loop.MinBy(p => Math.Abs(FieldBounds.SprayDeg(p.X, p.Z) + AtBatResolver.FoulLineDeg));
        var right = loop.MinBy(p => Math.Abs(FieldBounds.SprayDeg(p.X, p.Z) - AtBatResolver.FoulLineDeg));
        Assert.Equal(lopsided.LeftFenceFt, FieldBounds.DistHome(left.X, left.Z), 9);
        Assert.Equal(lopsided.RightFenceFt, FieldBounds.DistHome(right.X, right.Z), 9);
        Assert.Equal(60, FieldBounds.DistHome(right.X, right.Z) - FieldBounds.DistHome(left.X, left.Z), 9);

        Assert.False(HarborWall.ParkIsSymmetric(lopsided));
        Assert.False(HarborWall.LoopIsSymmetric(lopsided),
            "a lopsided park must not draw one side's wall on the other (FD-06)");
        // Everything else the kit asks of a wall still holds; only the mirror is gone.
        Assert.True(HarborWall.WrapsTheDiamond(lopsided));
        Assert.True(HarborWall.OutfieldIsTheFence(lopsided));
        Assert.True(HarborWall.WrapStaysInFoul(lopsided));
        Assert.True(HarborWall.HomeWrapIsRound(lopsided));
        Assert.True(HarborDugout.WallMeetsTheRail(lopsided));
    }

    /// <summary>
    /// The loop is cached per park <b>and</b> per edge (#845). It was one slot keyed by the three
    /// posts: the flight asks for every park from every thread, so two parks in one process rebuilt
    /// it on every call, and a second <see cref="ParkBoundary"/> (F2-d) would have been served the
    /// first one's loop with nothing failing, because the posts matched. Same rail as
    /// <c>BoundaryTests.ADifferentEdgeIsADifferentPolygon</c>.
    /// </summary>
    [Fact]
    public void TheDrawnLoopIsCachedPerParkAndPerEdge()
    {
        var first = _content.Parks[_content.ParkPickOrder[0]];
        var second = _content.Parks[_content.ParkPickOrder[1]];
        var shipped = ParkBoundary.Default;

        var a = HarborWall.Loop(first);
        var b = HarborWall.Loop(second);
        Assert.NotSame(a, b);
        // Neither park evicts the other, and the default edge is the edge Loop(park) plays.
        Assert.Same(a, HarborWall.Loop(first));
        Assert.Same(b, HarborWall.Loop(second));
        Assert.Same(a, HarborWall.Loop(first, shipped));

        var wide = shipped with { FoulOffsetFt = shipped.FoulOffsetFt + 10 };
        var widened = HarborWall.Loop(first, wide);
        Assert.NotSame(a, widened);
        Assert.Same(widened, HarborWall.Loop(first, wide));
        // The point of the key: asking for another edge does not change the edge this park draws.
        Assert.Same(a, HarborWall.Loop(first));

        // And it is a different wall, not just a different array: the rail moved 10 ft into foul.
        Assert.Equal(shipped.FoulOffsetFt, RailOffsetFt(a), 6);
        Assert.Equal(wide.FoulOffsetFt, RailOffsetFt(widened), 6);
    }

    /// <summary>
    /// How far the drawn rail sits off the first-base line through the infield, where the flare has
    /// not started. That is <see cref="ParkBoundary.FoulOffsetFt"/>, drawn.
    /// </summary>
    static double RailOffsetFt((double X, double Z)[] loop)
    {
        var off = 0.0;
        foreach (var p in loop)
        {
            if (p.Z <= 0 || p.Z > HarborWall.RampStartZ || p.X <= 0) continue;
            off = Math.Max(off, (p.X - p.Z) / Math.Sqrt(2));
        }
        return off;
    }

    /// <summary>Distance from a drawn vertex to the nearest span of the flight polygon, and that span's kind.</summary>
    static (double Ft, FieldBounds.WallKind Kind) NearestSpan(FieldBounds.Boundary bounds, double x, double z)
    {
        var best = double.MaxValue;
        var kind = FieldBounds.WallKind.FoulWall;
        foreach (var s in bounds.Segments)
        {
            var ex = s.Bx - s.Ax;
            var ez = s.Bz - s.Az;
            var len2 = ex * ex + ez * ez;
            var t = len2 < 1e-12 ? 0 : Math.Clamp(((x - s.Ax) * ex + (z - s.Az) * ez) / len2, 0, 1);
            var px = s.Ax + ex * t;
            var pz = s.Az + ez * t;
            var d = Math.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz));
            if (d >= best) continue;
            best = d;
            kind = s.Kind;
        }
        return (best, kind);
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
