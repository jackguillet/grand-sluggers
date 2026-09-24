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
/// to left actually meets. F2-b2 (#873, FD-06-R2) made it true of the rail's top to the pole: each
/// drawn span stands at the top of the flight segment under it at each end (<see cref="HarborWall.SpanTops"/>),
/// so the rail stays hip-high to the pole and the wall steps up to the fence at the pole, where the
/// ball's does.
/// </para>
/// </summary>
public sealed class HarborWallTests
{
    readonly ContentCatalog _content = Shipped.Content;

    /// <summary>
    /// The catalog's own pick cycle (#820), not a literal, so a park added to <c>data/parks/</c> is
    /// covered the day it lands. <c>ParkSchemaTests</c> pins the same order.
    /// </summary>
    public static TheoryData<string> Parks()
    {
        var parks = new TheoryData<string>();
        foreach (var id in Shipped.Content.ParkPickOrder) parks.Add(id);
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

    /// <summary>
    /// The foul rail stays hip-high to the pole (FD-06-R2, Jack, September 22, 2026). Every drawn vertex
    /// that is not on the fence — the whole foul wrap and the backstop, the last feet before each pole
    /// included — stands at the flight's rail top, and the only change of height on the foul side is
    /// the step at each pole.
    ///
    /// <para>
    /// Re-authored by F2-b2 (#873). It used to exempt every vertex past a literal 95 ft out
    /// (<c>RampStartZ</c>) and ask instead that the rail ramp up to the fence there over at least six
    /// vertices (<c>TaperIsARamp</c>), while the ball's rail stayed hip-high to the pole. That literal
    /// broke on shorter foul lines: the two 8-ft parks drew four ramp vertices, not six. With the ramp
    /// gone there is no literal and no gap.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Parks))]
    public void TheFoulRailStaysHipHighToThePoleAndMatchesTheFlightsFoulWall(string id)
    {
        var park = _content.Parks[id];
        Assert.Equal(4.2f, HarborWall.HipHeight);
        Assert.Equal(HarborWall.HipHeight, FieldBounds.FoulWallHeightFt, 4);
        Assert.All(FieldBounds.Of(park).Segments.Where(s => s.Kind == FieldBounds.WallKind.FoulWall),
            s => Assert.Equal(FieldBounds.FoulWallHeightFt, s.HeightFt, 4));

        var rail = 0;
        var n = HarborWall.Loop(park).Length;
        for (var i = 0; i < n; i++)
        {
            if (HarborWall.IsOutfield(park, i)) continue;
            rail++;
            Assert.Equal(HarborWall.HipHeight, HarborWall.Height(park, i));
            Assert.Equal((HarborWall.HipHeight, HarborWall.HipHeight), HarborWall.SpanTops(park, i));
        }
        Assert.Equal(n - (HarborWall.OutfieldSegs + 1), rail);

        // The rail's last vertex before each pole: the old ramp's top end, now hip-high.
        foreach (var sign in new[] { -1, 1 })
        {
            var last = Enumerable.Range(0, n).Where(i => !HarborWall.IsOutfield(park, i))
                .MaxBy(i => sign * HarborWall.LoopPoint(park, i).X);
            var p = HarborWall.LoopPoint(park, last);
            Assert.True(FieldBounds.DistHome(p.X, p.Z) > 0.9 * AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg),
                $"{id}: the rail vertex nearest the {(sign < 0 ? "left" : "right")} pole is {FieldBounds.DistHome(p.X, p.Z):0.#} ft out");
            Assert.Equal(HarborWall.HipHeight, HarborWall.Height(park, last));
        }
        Assert.True(HarborWall.StepsOnlyAtThePoles(park), $"{id}: the wall steps from the rail to the fence at each pole and nowhere else");
    }

    /// <summary>
    /// Feet. Every drawn vertex is built from the same <see cref="AtBatResolver.FenceAt"/> sample or
    /// the same <see cref="ParkBoundary.RailPoint"/> the polygon is built from, so the gap is
    /// rounding and nothing else: the worst measured on either root is 5e-15 ft. A tolerance this
    /// tight is the point — "near the wall" is what the mirror looked like from center field.
    /// </summary>
    const double OnTheWallFt = 1e-9;

    /// <summary>
    /// <c>SF-05</c> (D15 as amended by D21, FD-06; #845; the whole rail since F2-b2, #873, FD-06-R2).
    /// The wall the player sees is the wall the ball meets, on every span of every park in the pick
    /// cycle:
    ///
    /// <list type="bullet">
    /// <item>every drawn vertex lies on <see cref="FieldBounds.Of(Park)"/>'s polygon;</item>
    /// <item>every drawn span lies on one flight segment and is drawn at that segment's top — the
    /// fence's on a fair span (D15), the rail's on a foul span, all the way to the pole;</item>
    /// <item>every drawn vertex stands at the tallest flight segment it lies on, so a pole is the
    /// fence's end and every other foul vertex is the rail;</item>
    /// <item>the wall steps from the fence to the rail at each pole, where the flight's segment kind
    /// changes, and nowhere else;</item>
    /// <item>each pole is drawn at <i>that side's</i> fence distance — which is the mirror #845
    /// removed: before it a lopsided park drew its right-field pole in left field.</item>
    /// </list>
    ///
    /// <para>
    /// Until F2-b2 the row stopped at <c>HarborWall.RampStartZ</c> (95 ft out): past it the drawn rail
    /// ramped up to the fence while the flight's rail stayed hip-high to the pole. Jack chose the
    /// flight's rail (FD-06-R2), so the drawing follows and the row claims that span too.
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

        var (fair, rail) = AssertDrawnIsFlight(id, park, bounds, loop);
        Assert.Equal(HarborWall.OutfieldSegs + 1, fair);
        Assert.Equal(loop.Length - fair, rail);
        // The rail the kit draws through the infield stands off the line at the edge's own offset.
        Assert.Equal(ParkBoundary.Default.FoulOffsetFt, RailOffsetFt(loop), 6);

        // Each pole at its own distance, and the step at it. The mirror drew the right-field pole on both sides.
        foreach (var sign in new[] { -1, 1 })
            AssertTheStepIsAtThePole(id, park, sign);
        Assert.Equal(park.LeftFenceFt == park.RightFenceFt, HarborWall.ParkIsSymmetric(park));
    }

    /// <summary>
    /// Every drawn vertex on the flight polygon, every drawn span on one flight segment and drawn at
    /// that segment's top at each of its ends, every drawn vertex at the tallest segment it lies on.
    /// A level segment's top is asserted to the bit; a polyline span's sloped top (F2-c) to 1e-4 ft.
    /// Returns the vertices on the fence (the poles included) and the vertices on the rail and backstop
    /// only.
    /// </summary>
    static (int Fair, int Rail) AssertDrawnIsFlight(string id, Park park, FieldBounds.Boundary bounds, (double X, double Z)[] loop)
    {
        var fair = 0;
        var rail = 0;
        for (var i = 0; i < loop.Length; i++)
        {
            var p = loop[i];
            var q = loop[(i + 1) % loop.Length];
            var at = SpansThrough(bounds, p);
            Assert.True(at.Length > 0,
                $"{id}: drawn vertex {i} at ({p.X:0.###}, {p.Z:0.###}) is {NearestSpan(bounds, p.X, p.Z).Ft:0.###} ft off the flight wall");

            // The span from this vertex to the next is a piece of one flight segment, drawn at its top at each end.
            var under = SpansThrough(bounds, p).Intersect(SpansThrough(bounds, q)).ToArray();
            Assert.True(under.Length > 0, $"{id}: drawn span {i} does not lie on one flight segment");
            Assert.Single(under);
            var seg = under[0];
            var tops = HarborWall.SpanTops(park, i);
            Assert.Equal(seg.Kind, HarborWall.FlightSpan(park, i).Kind);
            if (seg.HeightBFt is null)
            {
                Assert.Equal((float)seg.HeightFt, tops.Start);
                Assert.Equal((float)seg.HeightFt, tops.End);
            }
            else
            {
                Assert.Equal(seg.HeightAt(Along(seg, p)), tops.Start, 4);
                Assert.Equal(seg.HeightAt(Along(seg, q)), tops.End, 4);
            }

            // The vertex stands at the tallest flight wall that meets there.
            var tallest = at.Max(s => s.HeightAt(Along(s, p)));
            if (at.All(s => s.HeightBFt is null))
                Assert.Equal((float)tallest, HarborWall.Height(park, i));
            else
                Assert.Equal(tallest, HarborWall.Height(park, i), 4);
            var onFence = at.Any(s => s.Kind == FieldBounds.WallKind.FairFence);
            Assert.Equal(onFence, HarborWall.IsOutfield(park, i));
            if (onFence)
            {
                fair++;
                Assert.Equal(AtBatResolver.FenceSpotAt(park, FieldBounds.SprayDeg(p.X, p.Z)).TopFt, HarborWall.Height(park, i), 4);
            }
            else
            {
                rail++;
                Assert.Equal(FieldBounds.FoulWallHeightFt, HarborWall.Height(park, i), 4);
            }
        }
        return (fair, rail);
    }

    /// <summary>
    /// The pole on this side is a drawn vertex at that side's fence distance, the fence runs to it at
    /// the fence's top there (the park's <c>fenceHeightFt</c>, or a polyline's pole point, F2-c), and the
    /// rail leaves it at the rail's top: the step is at the pole, on the vertex where the flight's segment
    /// kind changes, and nowhere near it is a ramp.
    /// </summary>
    static void AssertTheStepIsAtThePole(string id, Park park, int sign)
    {
        var loop = HarborWall.Loop(park);
        var want = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
        var pole = Enumerable.Range(0, loop.Length)
            .MinBy(i => Math.Abs(FieldBounds.SprayDeg(loop[i].X, loop[i].Z) - sign * AtBatResolver.FoulLineDeg));
        var at = loop[pole];
        Assert.Equal(want, FieldBounds.DistHome(at.X, at.Z), 9);
        Assert.True(HarborWall.IsPole(park, pole), $"{id}: vertex {pole} is not where the kit stands the pole");

        // Walking the loop, the right pole is fence → rail and the left pole is rail → fence.
        var (fenceSide, railSide) = sign > 0 ? (pole - 1, pole) : (pole, pole - 1);
        Assert.Equal(FieldBounds.WallKind.FairFence, HarborWall.FlightSpan(park, fenceSide).Kind);
        Assert.Equal(FieldBounds.WallKind.FoulWall, HarborWall.FlightSpan(park, railSide).Kind);
        var fenceTop = (float)AtBatResolver.FenceSpotAt(park, sign * AtBatResolver.FoulLineDeg).TopFt;
        var fenceSpan = HarborWall.SpanTops(park, fenceSide);
        Assert.Equal(fenceTop, sign > 0 ? fenceSpan.End : fenceSpan.Start);
        Assert.Equal((HarborWall.HipHeight, HarborWall.HipHeight), HarborWall.SpanTops(park, railSide));
        Assert.Equal(fenceTop, HarborWall.Height(park, pole));
        // One vertex into foul the wall is already the rail: the step is not spread over a sample.
        var intoFoul = sign > 0 ? pole + 1 : pole - 1;
        Assert.False(HarborWall.IsOutfield(park, intoFoul), $"{id}: vertex {intoFoul} past the pole is drawn as fence");
        Assert.Equal(HarborWall.HipHeight, HarborWall.Height(park, intoFoul));
    }

    /// <summary>How far along a flight segment (0 at A, 1 at B) a point on it stands.</summary>
    static double Along(FieldBounds.WallSegment s, (double X, double Z) p)
    {
        var ex = s.Bx - s.Ax;
        var ez = s.Bz - s.Az;
        var len2 = ex * ex + ez * ez;
        return len2 < 1e-12 ? 0 : Math.Clamp(((p.X - s.Ax) * ex + (p.Z - s.Az) * ez) / len2, 0, 1);
    }

    /// <summary>The flight segments a drawn point lies on (the tolerance is <see cref="OnTheWallFt"/>).</summary>
    static FieldBounds.WallSegment[] SpansThrough(FieldBounds.Boundary bounds, (double X, double Z) p) =>
        bounds.Segments.Where(s => DistToSpan(s, p.X, p.Z) <= OnTheWallFt).ToArray();

    /// <summary>
    /// <c>SF-05</c> on a park the catalog does not carry: a fixture pulled 30 ft in on one line and
    /// 30 ft out on the other, so the row proves the builder and not today's data. On the shipped
    /// root that is 300 / 400 / 360, and before #845 its drawn left field stood at 360 ft — the
    /// right-field wall, mirrored — with every gate above it green. Since F2-b2 (FD-06-R2) it also
    /// holds the whole rail and the step on a park whose two poles stand at different distances: each
    /// side's rail runs hip-high to its own pole and steps up there.
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
        var (fair, rail) = AssertDrawnIsFlight(lopsided.Id, lopsided, bounds, loop);
        Assert.Equal(HarborWall.OutfieldSegs + 1, fair);
        Assert.Equal(loop.Length - fair, rail);
        foreach (var sign in new[] { -1, 1 })
            AssertTheStepIsAtThePole(lopsided.Id, lopsided, sign);
        Assert.True(HarborWall.StepsOnlyAtThePoles(lopsided));

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
    /// <c>SF-05</c> on a polyline fence (F2-c #874; FD-06 C, D15 as amended by D21). No catalog park names
    /// points, so this is a fixture on Harbor: a tall porch down the left line, an alley, a low corner. The drawn
    /// loop walks the bearings the clip polygon is built on, so every fence point is a drawn vertex exactly where the
    /// polygon has it, every drawn vertex lies on the flight wall, and every outfield vertex is drawn at the flight's top
    /// at that vertex — the point's own height at a point, the straight line between two heights along a sloped span.
    /// The loop grows by the points that are not already on the spray grid, and nothing else about it changes.
    /// </summary>
    [Fact]
    public void SF05_APolylineParkDrawsEveryPointAndEverySpanAtTheFlightsTop()
    {
        FencePoint[] points =
        [
            new(-45, 0.9, 14), new(-20, 0.9, 14), new(-12, 1.05, 10), new(0, 1.0, 12),
            new(10, 0.95, 12), new(27.5, 1.0, 8), new(45, 1.0, 12)
        ];
        var catalog = _content;
        var harbor = catalog.Parks[HarborPostcard.ParkId];
        var park = harbor with { Id = "sf05-polyline", Fence = new ParkFence(points) };
        var loop = HarborWall.Loop(park);
        var bounds = FieldBounds.Of(park);
        var offGrid = points.Count(p => !FieldBounds.FenceBearings(harbor).Contains(p.BearingDeg));
        Assert.Equal(4, offGrid);
        Assert.Equal(HarborWall.WrapSegs + offGrid, loop.Length);

        foreach (var p in points)
        {
            var at = BallFlight.GroundPoint(p.FenceFrac * AtBatResolver.FenceAt(harbor, p.BearingDeg), p.BearingDeg);
            var i = Array.IndexOf(loop, at);
            Assert.True(i >= 0, $"{catalog.Root.Provenance}: the point at {p.BearingDeg} degrees is not a drawn vertex");
            Assert.Equal(p.HeightFt, HarborWall.Height(park, i), 4);
        }

        var sloped = 0;
        for (var i = 0; i < loop.Length; i++)
        {
            var (dist, piece, along) = NearestPiece(bounds, loop[i].X, loop[i].Z);
            Assert.True(dist <= OnTheWallFt, $"drawn vertex {i} is {dist:0.###} ft off the flight wall");
            if (piece.Kind != FieldBounds.WallKind.FairFence) continue;
            Assert.True(HarborWall.IsOutfield(park, i), $"vertex {i} is on a fair span but is not drawn as outfield");
            Assert.Equal(piece.HeightAt(along), HarborWall.Height(park, i), 4);
            if (piece.HeightBFt is not null) sloped++;
        }
        Assert.True(sloped > 4, $"{sloped} drawn vertices on a sloped span");
        Assert.True(HarborWall.OutfieldIsTheFence(park));

        // F2-b2 (FD-06-R2) on the same fixture: every drawn span is its flight segment's top at both its ends, so a
        // sloped span is drawn sloped between its two point heights and every other span level; the rail stays hip-high
        // to each pole and steps up to that pole's own point height (14 ft on the left, 12 ft on the right).
        var (fair, rail) = AssertDrawnIsFlight(park.Id, park, bounds, loop);
        Assert.Equal(FieldBounds.FenceBearings(park).Count, fair);
        Assert.Equal(loop.Length - fair, rail);
        var slopedSpans = Enumerable.Range(0, loop.Length).Count(i => HarborWall.SpanTops(park, i).Start != HarborWall.SpanTops(park, i).End);
        Assert.Equal(bounds.Segments.Count(sg => sg.HeightBFt is not null), slopedSpans);
        foreach (var sign in new[] { -1, 1 })
            AssertTheStepIsAtThePole(park.Id, park, sign);
        Assert.True(HarborWall.StepsOnlyAtThePoles(park));

        // The poles stand where the polyline starts and ends; the drawn rail meets the fence there.
        foreach (var (sign, p) in new[] { (-1, points[0]), (1, points[^1]) })
        {
            var pole = loop.MinBy(v => Math.Abs(FieldBounds.SprayDeg(v.X, v.Z) - sign * AtBatResolver.FoulLineDeg));
            Assert.Equal(p.FenceFrac * AtBatResolver.FenceAt(harbor, p.BearingDeg), FieldBounds.DistHome(pole.X, pole.Z), 9);
        }
    }

    /// <summary>The nearest piece of the flight polygon to a drawn vertex, how far it is, and how far along the piece.</summary>
    static (double Ft, FieldBounds.WallSegment Piece, double Along) NearestPiece(FieldBounds.Boundary bounds, double x, double z)
    {
        var best = double.MaxValue;
        FieldBounds.WallSegment? piece = null;
        var along = 0.0;
        foreach (var s in bounds.Segments)
        {
            var ex = s.Bx - s.Ax;
            var ez = s.Bz - s.Az;
            var len2 = ex * ex + ez * ez;
            var t = len2 < 1e-12 ? 0 : Math.Clamp(((x - s.Ax) * ex + (z - s.Az) * ez) / len2, 0, 1);
            var d = Diamond.Dist(x, z, s.Ax + ex * t, s.Az + ez * t);
            if (d >= best) continue;
            (best, piece, along) = (d, s, t);
        }
        return (best, piece!, along);
    }

    /// <summary>
    /// The tops the FD-06-R2 look gate was captured on (PR #875 at <c>21091d3a</c>, before F2-c merged),
    /// for every catalog park, as run-lengths of float values around the loop from center field: each
    /// vertex's <see cref="HarborWall.Height"/>, and each span's <see cref="HarborWall.SpanTops"/> at both
    /// ends. The fence from center field to the right pole (vertices 0–24), the rail to the left pole
    /// (25–83), the fence back to center field (84–107); every span level. Rebasing onto F2-c's per-end tops must not move a stroke of
    /// what Jack was shown; the drawn positions are pinned to the bit by
    /// <c>PolylineFenceTests.SF06_EveryCatalogParkPlaysAndDrawsTheThreePostFenceBitForBit</c>.
    /// </summary>
    [Theory]
    [InlineData(ParkId.Harbor, "12")]
    [InlineData(ParkId.Crystal, "8")]
    [InlineData(ParkId.Funfair, "8")]
    [InlineData(ParkId.Rooftop, "12")]
    [InlineData(ParkId.Canopy, "12")]
    [InlineData(ParkId.Ember, "10")]
    public void TheCatalogParksDrawTheTopsTheLookGateWasShown(string id, string fence)
    {
        var park = _content.Parks[id];
        var n = HarborWall.Loop(park).Length;
        Assert.Equal(108, n);
        var vertices = Enumerable.Range(0, n).Select(i => HarborWall.Height(park, i));
        var starts = Enumerable.Range(0, n).Select(i => HarborWall.SpanTops(park, i).Start);
        var ends = Enumerable.Range(0, n).Select(i => HarborWall.SpanTops(park, i).End);
        Assert.Equal($"{fence}x25 4.2x59 {fence}x24", Runs(vertices));
        Assert.Equal($"{fence}x24 4.2x60 {fence}x24", Runs(starts));
        Assert.Equal($"{fence}x24 4.2x60 {fence}x24", Runs(ends));
    }

    /// <summary>Run-lengths of exact float values ("12x25 4.2x59 ..."), compared bit for bit.</summary>
    static string Runs(IEnumerable<float> values)
    {
        var runs = new List<(float Value, int Count)>();
        foreach (var v in values)
        {
            if (runs.Count > 0 && BitConverter.SingleToInt32Bits(runs[^1].Value) == BitConverter.SingleToInt32Bits(v))
                runs[^1] = (v, runs[^1].Count + 1);
            else
                runs.Add((v, 1));
        }
        return string.Join(" ", runs.Select(r => r.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "x" + r.Count));
    }

    /// <summary>
    /// The kit draws what these rows pin (FD-06-R2). Unity has no EditMode test assembly and
    /// <c>unity/</c> is not in the solution, so this reads <c>FieldKit.Wall</c>'s source the way
    /// <c>FieldKitSourceTests</c> does: each span is drawn from its own flight top at each end
    /// (<see cref="HarborWall.SpanTops"/>) — level where the two are equal, which is every span of every
    /// catalog park, and sloped along a polyline span whose two points differ (F2-c) — not from its two
    /// end vertices' <see cref="HarborWall.Height"/>. Reading the vertices would draw the span that
    /// leaves each pole — a fence vertex, then rail — as a steep ramp over one sample instead of the
    /// step at the pole.
    /// </summary>
    [Fact]
    public void TheKitDrawsEachSpanAtItsOwnFlightTop()
    {
        var repo = Path.GetFullPath(Path.Combine(_content.Root.Shipped, ".."));
        var src = File.ReadAllText(Path.Combine(repo, "unity/Assets/Scripts/Runtime/FieldKit.cs"));
        var from = src.IndexOf("public void Wall(", StringComparison.Ordinal);
        var to = src.IndexOf("static void RampPrism(", StringComparison.Ordinal);
        Assert.True(from >= 0 && to > from, "FieldKit.Wall and RampPrism are where this row reads them");
        var wall = src.Substring(from, to - from);
        Assert.Contains("HarborWall.SpanTops(park, i)", wall, StringComparison.Ordinal);
        Assert.DoesNotContain("HarborWall.Height(", wall, StringComparison.Ordinal);
        Assert.Contains("RampPrism(folder, \"Wall\" + i, a, b, h0, h1,", wall, StringComparison.Ordinal);
        Assert.Contains("RampCap(folder, \"Cap\" + i, a, b, h0, h1,", wall, StringComparison.Ordinal);
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
    /// not started (along the line up to <see cref="ParkBoundary.FlareStartFt"/>; along is
    /// (x + z) / √2 on the first-base side). That is <see cref="ParkBoundary.FoulOffsetFt"/>, drawn.
    /// Until F2-b2 the stretch was cut at <c>HarborWall.RampStartZ</c>, which went with the ramp.
    /// </summary>
    static double RailOffsetFt((double X, double Z)[] loop)
    {
        var flareStart = ParkBoundary.Default.FlareStartFt;
        var off = 0.0;
        foreach (var p in loop)
        {
            if (p.Z <= 0 || p.X <= 0 || (p.X + p.Z) / Math.Sqrt(2) > flareStart + 1e-9) continue;
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
            var d = DistToSpan(s, x, z);
            if (d >= best) continue;
            best = d;
            kind = s.Kind;
        }
        return (best, kind);
    }

    static double DistToSpan(FieldBounds.WallSegment s, double x, double z)
    {
        var ex = s.Bx - s.Ax;
        var ez = s.Bz - s.Az;
        var len2 = ex * ex + ez * ez;
        var t = len2 < 1e-12 ? 0 : Math.Clamp(((x - s.Ax) * ex + (z - s.Az) * ez) / len2, 0, 1);
        var px = s.Ax + ex * t;
        var pz = s.Az + ez * t;
        return Math.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz));
    }

    [Fact]
    public void HarborStandsAtTwelveFeet()
    {
        // The value is Jack's call (D15); twelve is the recommendation: taller than MLB's eight so a rob and a
        // carom read, far under the old 26-ft dressing so a deep fly is still a homer.
        Assert.Equal(12, _content.Parks[ParkId.Harbor].FenceHeightFt);
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
        var harbor = _content.Parks[ParkId.Harbor];
        Assert.False(HarborWall.OutfieldIsTheFence(harbor with { FenceHeightFt = HarborWall.HipHeight }));
    }
}
