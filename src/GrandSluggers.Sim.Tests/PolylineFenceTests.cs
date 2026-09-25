using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F2-c (#874; FD-06 C, FD-06-R1, FD-12 B; FR-02, FR-03, FR-05, FR-06): the polyline fence (spec §6.1, §7.9, §16;
/// Appendix B.9 <c>SF-06</c>, <c>SF-07</c>, the per-span half of <c>SF-12</c>).
///
/// <para>
/// <b>Two kinds of row.</b> <c>SF06_…</c> is parity: no shipped park names points, so every catalog park must
/// play and draw the three-post fence it always did, held <em>bit for bit</em> against the code as it stood before the
/// polyline existed (<see cref="Old"/>, transcribed from <c>2f1d56ba</c> — the before picture, which nothing here may
/// "fix"). The <c>SF07_…</c> and <c>SF12_…</c> rows prove the rail is real on a fixture fence built in the test — a porch,
/// a notch and a short alley — because the catalog cannot: with no points, a reader that ignored the polyline and one
/// that followed it look the same.
/// </para>
///
/// </summary>
public sealed class PolylineFenceTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    /// <summary>
    /// The fixture fence: a tall short porch down the left-field line, back out to the arc, a notch at centre, and a short,
    /// deeper, lower alley in right-centre. Fractions of each park's own three-post fence (FD-12), so the one block is
    /// legal on any park's posts; heights in feet, unscaled. The alley's two spans name <c>padded</c> out loud, the rest leave
    /// it to the default.
    /// </summary>
    static readonly FencePoint[] Fixture =
    [
        new(-45, 0.92, 16),
        new(-31, 0.92, 16),
        new(-29, 1.0, 12),
        new(-4, 1.0, 12),
        new(0, 0.93, 12),
        new(4, 1.0, 12),
        new(16, 1.0, 12, WallMaterial.Padded),
        new(22, 1.10, 9, WallMaterial.Padded),
        new(28, 1.10, 9),
        new(34, 1.0, 12),
        new(45, 1.0, 12)
    ];

    static Park Fenced(Park park, IEnumerable<FencePoint>? points = null) =>
        park with { Id = park.Id + "-sf07", Fence = new ParkFence((points ?? Fixture).ToList()) };

    /// <summary>Where a point stands: its fraction of the park's own three-post fence at its bearing (FD-12).</summary>
    static double PointFt(Park park, FencePoint point) =>
        point.FenceFrac * AtBatResolver.FenceAt(park with { Fence = null }, point.BearingDeg);

    static (double X, double Z) PointAt(Park park, FencePoint point) => BallFlight.GroundPoint(PointFt(park, point), point.BearingDeg);

    // ---------------------------------------------------------------------------------
    // SF-06 — a park that names no points plays and draws exactly as today
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-06</c>, every catalog park: no park names points, and <see cref="AtBatResolver.FenceAt"/> on a
    /// fine bearing grid (inside and past both poles), the clip polygon vertex for vertex, and the drawn loop are the
    /// pre-polyline ones <b>to the bit</b>. Every piece of the polygon is level (no far top) and padded, its top at any
    /// point along it is its <see cref="FieldBounds.WallSegment.HeightFt"/> exactly, and a seeded match at the park writes
    /// no <c>fence</c> key into its trace identity, so no stored identity moves. The CLI's <c>match --seed 7</c> and the
    /// park-factors table, before and after, are in the PR.
    /// </summary>
    [Fact]
    public void SF06_EveryCatalogParkPlaysAndDrawsTheThreePostFenceBitForBit()
    {
        var parks = 0;
        var catalog = Game;
        // Every park that names no points (F9-a: Crystal names its glass boards and is held by F9A_CrystalsFence…).
        Assert.Equal([ParkId.Crystal], catalog.Parks.Values.Where(p => p.Fence is not null).Select(p => p.Id));
        foreach (var park in catalog.Parks.Values.Where(p => p.Fence is null))
        {
            var (home, away) = PresetTeams.Pair(catalog, "rio", "ashlord");
            parks++;
            Assert.Null(park.Fence);

            for (var tenth = -500; tenth <= 500; tenth++)
            {
                var bearing = tenth / 10.0;
                Bits($"{park.Id} FenceAt({bearing})", Old.FenceAt(park, bearing), AtBatResolver.FenceAt(park, bearing));
                var spot = AtBatResolver.FenceSpotAt(park, bearing);
                Bits($"{park.Id} spot({bearing})", Old.FenceAt(park, bearing), spot.DistanceFt);
                Assert.Equal(park.FenceHeightFt, spot.TopFt);
                Assert.Equal(WallMaterial.Padded, spot.Material);
            }

            var grid = Enumerable.Range(0, FieldBounds.FenceSegs + 1)
                .Select(i => -AtBatResolver.FoulLineDeg + 2 * AtBatResolver.FoulLineDeg * i / FieldBounds.FenceSegs).ToArray();
            Assert.Equal(grid, FieldBounds.FenceBearings(park));

            var expected = Old.Polygon(park, ParkBoundary.Default);
            var actual = FieldBounds.Of(park, Rules.Default).Segments;
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                // Record equality: the positions, the top, the kind, the normal, the radii — and the two new members at
                // their defaults, padded and level.
                Assert.Equal(expected[i], actual[i]);
                Assert.Null(actual[i].HeightBFt);
                Assert.Equal(WallMaterial.Padded, WallMaterial.OfSegment(actual[i]));
                Bits($"{park.Id} top of segment {i}", actual[i].HeightFt, actual[i].HeightAt(0.37));
            }

            var oldLoop = Old.Loop(park, ParkBoundary.Default);
            var loop = HarborWall.Loop(park);
            Assert.Equal(oldLoop.Length, loop.Length);
            for (var i = 0; i < loop.Length; i++)
            {
                Bits($"{park.Id} loop[{i}].X", oldLoop[i].X, loop[i].X);
                Bits($"{park.Id} loop[{i}].Z", oldLoop[i].Z, loop[i].Z);
                if (HarborWall.IsOutfield(park, i))
                    Assert.Equal((float)park.FenceHeightFt, HarborWall.Height(park, i));
            }

            var identity = PlayTraceIdentity.Capture(new Match(catalog, away, home, park, innings: 3, seed: 7));
            Assert.DoesNotContain("\"fence\"", identity.InputsJson, StringComparison.Ordinal);
        }
        Assert.Equal(ParkId.All.Count - 1, parks); // every park less Aurora Rink, which names its glass boards (F9-a)
    }

    // ---------------------------------------------------------------------------------
    // SF-07 — a notch, a porch and an alley: the polygon, one distance per bearing, and every reader
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-07</c>. On the fixture fence every point is a vertex of the clip polygon, exactly where FD-12's
    /// unit puts it; each fence piece carries the top at both its ends (the point heights, straight between them) and the
    /// material of its span; and the pieces between the poles are the fence and nothing else.
    /// </summary>
    [Fact]
    public void SF07_EveryPointIsAVertexOfTheClipPolygonAtItsHeight()
    {
        var catalog = Game;
        var park = Fenced(catalog.Parks[ParkId.Harbor]);
        var fair = FieldBounds.Of(park, Rules.Default).Segments.Where(s => s.Kind == FieldBounds.WallKind.FairFence).ToList();
        Assert.Equal(FieldBounds.FenceBearings(park).Count - 1, fair.Count);
        // The fence runs pole to pole: the first piece starts on the left pole, the last ends on the right.
        Assert.Equal(PointAt(park, Fixture[0]), (fair[0].Ax, fair[0].Az));
        Assert.Equal(PointAt(park, Fixture[^1]), (fair[^1].Bx, fair[^1].Bz));

        for (var k = 0; k < Fixture.Length; k++)
        {
            var at = PointAt(park, Fixture[k]);
            var starts = fair.Where(s => (s.Ax, s.Az) == at).ToList();
            var ends = fair.Where(s => (s.Bx, s.Bz) == at).ToList();
            Assert.True(starts.Count + ends.Count > 0, $"{catalog.Root.Provenance}: point {k} is not a polygon vertex");
            foreach (var s in starts)
            {
                Assert.Equal(Fixture[k].HeightFt, s.HeightFt);
                Assert.Equal(Fixture[k].Material ?? WallMaterial.Padded, s.Material);
            }
            foreach (var s in ends)
                Near(Fixture[k].HeightFt, s.HeightAt(1), 1e-9);
        }

        // Between two points a piece is straight on the chord, its top on the straight line between the heights.
        foreach (var s in fair)
        {
            var span = SpanOf(park, FieldBounds.SprayDeg((s.Ax + s.Bx) / 2, (s.Az + s.Bz) / 2));
            var (a, b) = (PointAt(park, Fixture[span]), PointAt(park, Fixture[span + 1]));
            Assert.True(OffLine(a, b, s.Ax, s.Az) < 1e-9 && OffLine(a, b, s.Bx, s.Bz) < 1e-9, $"piece off span {span}'s chord");
            foreach (var along in new[] { 0.0, 0.5, 1.0 })
            {
                var x = s.Ax + (s.Bx - s.Ax) * along;
                var z = s.Az + (s.Bz - s.Az) * along;
                Near(TopOnChord(park, span, x, z), s.HeightAt(along), 1e-9);
            }
            Assert.Equal(Fixture[span].Material ?? WallMaterial.Padded, WallMaterial.OfSegment(s));
        }
    }

    /// <summary>
    /// <c>SF-07</c>, FD-06-R1: one distance per bearing, by construction. On a 0.01° grid from pole to pole
    /// the ray from home meets exactly one span of the fixture (two only on a point's own bearing, where they share the
    /// point), <see cref="AtBatResolver.FenceAt"/> is that one distance — computed here independently from the points —
    /// and it is the distance the clip polygon's own <see cref="FieldBounds.Boundary.RadiusAt"/> reads. The track, the
    /// two poles and the zone map follow it without knowing it is a polyline.
    /// </summary>
    [Fact]
    public void SF07_FenceAtIsSingleValuedAndTheTrackThePolesAndTheZoneMapFollowIt()
    {
        var catalog = Game;
        var arc = catalog.Parks[ParkId.Harbor];
        var park = Fenced(arc);
        var polygon = FieldBounds.Of(park, Rules.Default);
        for (var hundredth = -4500; hundredth <= 4500; hundredth++)
        {
            var bearing = hundredth / 100.0;
            var hits = new List<double>();
            for (var k = 0; k + 1 < Fixture.Length; k++)
                if (RayMeetsChord(PointAt(park, Fixture[k]), PointAt(park, Fixture[k + 1]), bearing) is { } d)
                    hits.Add(d);
            Assert.NotEmpty(hits);
            Assert.True(hits.Max() - hits.Min() < 1e-9, $"bearing {bearing}: the ray meets the fence at {string.Join(", ", hits)}");
            Assert.True(hits.Count == 1 || Fixture.Any(p => p.BearingDeg == bearing), $"bearing {bearing}: {hits.Count} spans");

            var fence = AtBatResolver.FenceAt(park, bearing);
            Near(hits[0], fence, 1e-9);
            Near(polygon.RadiusAt(bearing), fence, 1e-9);
        }
        // Past a pole the fence is the pole, as it always was.
        Assert.Equal(PointFt(park, Fixture[0]), AtBatResolver.FenceAt(park, -60));
        Assert.Equal(PointFt(park, Fixture[^1]), AtBatResolver.FenceAt(park, 60));

        // The poles stand where the polyline starts and ends; the porch pulled the left one in.
        Assert.Equal(PointAt(park, Fixture[0]), ParkDiamond.FoulPole(park, -1));
        Assert.Equal(PointAt(park, Fixture[^1]), ParkDiamond.FoulPole(park, 1));
        Assert.True(FieldBounds.DistHome(ParkDiamond.FoulPole(park, -1).X, ParkDiamond.FoulPole(park, -1).Z) < arc.LeftFenceFt * 0.95);

        // The track is a track width inside the fence at every bearing, the notch's bottom included.
        foreach (var bearing in new[] { -38.0, -30, 0, 25 })
        {
            var inner = ParkDiamond.TrackInner(park, bearing);
            Near(AtBatResolver.FenceAt(park, bearing) - ParkDiamond.TrackWidth, FieldBounds.DistHome(inner.X, inner.Z), 1e-9);
        }

        // The zone map reads the notch and the alley: a point 5 ft inside the notch is the track, where the arc would
        // have left it on the grass; a point 2 ft past the arc in the alley is still the outfield grass.
        var zones = GroundZones.Of(park, catalog.Rules);
        var plain = GroundZones.Of(arc, catalog.Rules);
        var notch = BallFlight.GroundPoint(AtBatResolver.FenceAt(park, 0) - 5, 0);
        Assert.Equal(GroundZone.WarningTrack, zones.ZoneAt(notch.X, notch.Z));
        Assert.Equal(GroundZone.Outfield, plain.ZoneAt(notch.X, notch.Z));
        var alley = BallFlight.GroundPoint(AtBatResolver.FenceAt(arc, 25) + 2, 25);
        Assert.Equal(GroundZone.Outfield, zones.ZoneAt(alley.X, alley.Z));
        Assert.Equal(GroundZone.WarningTrack, plain.ZoneAt(alley.X, alley.Z));
    }

    /// <summary>
    /// <c>SF-07</c>: a ball rolled straight out into the notch's left face — a face that is not square to home —
    /// caroms off that span's own normal, not the arc's radial one. The crossing is on a piece of span 3 (−4° → 0°), the
    /// piece's outward normal is the chord's, and the speeds read off the samples come back mirrored about it at the
    /// span's row: into the wall × restitution, along it × tangential.
    /// </summary>
    [Fact]
    public void SF07_ABallRolledIntoTheNotchCaromsByThatSpansNormal()
    {
        var catalog = Game;
        var rules = catalog.Rules;
        var park = Fenced(catalog.Parks[ParkId.Harbor]);
        var dt = 1.0 / rules.Flight.SampleHz;
        const double bearing = -2;
        var fence = AtBatResolver.FenceAt(park, bearing);
        var (x0, z0) = BallFlight.GroundPoint(fence - 6, bearing);
        var (ux, uz) = BallFlight.GroundPoint(1, bearing);
        var path = BallFlight.Continue([], 0, x0, 0, z0, ux * 40, 0, uz * 40, 0, 90, park, rules);
        var hit = Enumerable.Range(1, path.Count - 1).First(i => path[i].Event == SampleEvent.Wall);
        Assert.True(hit >= 2 && hit + 1 < path.Count);

        var zones = GroundZones.Of(park, rules);
        var (inX, inZ) = Velocity(path, hit - 1, dt);
        var inSpeed = Math.Sqrt(inX * inX + inZ * inZ);
        var k = (inSpeed - zones.RowAt(path[hit - 1].X, path[hit - 1].Z, rules.Grounds).Roll.Friction * dt) / inSpeed;
        (inX, inZ) = (inX * k, inZ * k);
        var crossing = FieldBounds.Of(park, Rules.Default).Cross(path[hit - 1].X, path[hit - 1].Z, path[hit - 1].X + inX * dt, path[hit - 1].Z + inZ * dt);
        Assert.NotNull(crossing);
        var n = crossing.Value.Segment;
        Assert.Equal(FieldBounds.WallKind.FairFence, n.Kind);

        // The notch's left face, and its normal: the chord's, well off the radial line the arc would have had.
        var (a, b) = (PointAt(park, Fixture[3]), PointAt(park, Fixture[4]));
        Assert.True(OffLine(a, b, crossing.Value.X, crossing.Value.Z) < 1e-9, "the ball met the notch's left face");
        var (cx, cz) = (b.X - a.X, b.Z - a.Z);
        var len = Math.Sqrt(cx * cx + cz * cz);
        var (nx, nz) = (cz / len, -cx / len);
        if (nx * a.X + nz * a.Z < 0) (nx, nz) = (-nx, -nz);
        Near(nx, n.Nx, 1e-12);
        Near(nz, n.Nz, 1e-12);
        Assert.True(Math.Abs(n.Nx * ux + n.Nz * uz) < Math.Cos(10 * Math.PI / 180), "the face is not square to home");

        var (outX, outZ) = Velocity(path, hit + 1, dt);
        var outSpeed = Math.Sqrt(outX * outX + outZ * outZ);
        var back = (outSpeed + zones.RowAt(path[hit].X, path[hit].Z, rules.Grounds).Roll.Friction * dt) / outSpeed;
        (outX, outZ) = (outX * back, outZ * back);
        var row = rules.Walls.Of(WallMaterial.OfSegment(n));
        var inNormal = inX * n.Nx + inZ * n.Nz;
        var outNormal = outX * n.Nx + outZ * n.Nz;
        Assert.True(inNormal > 5);
        Assert.True(Math.Abs(outNormal + row.Restitution * inNormal) < 1e-6, $"normal {inNormal} came back {outNormal}");
        Assert.True(Math.Abs((outX - outNormal * n.Nx) - row.Tangential * (inX - inNormal * n.Nx)) < 1e-6
                    && Math.Abs((outZ - outNormal * n.Nz) - row.Tangential * (inZ - inNormal * n.Nz)) < 1e-6, "along the face");
        // The tangent is not zero: the ball came in straight from home and leaves along the slanted face.
        Assert.True(Math.Abs(outX - outNormal * n.Nx) + Math.Abs(outZ - outNormal * n.Nz) > 1);
    }

    /// <summary>
    /// The flight clears the polyline's own top: a fly that crosses the tall porch 2 ft under its 16-ft top caroms, and the
    /// same fly over the 9-ft alley 2 ft over its top is gone. The crossing's top is the piece's top where the ball met it
    /// (<see cref="FieldBounds.Crossing.HeightFt"/>), straight between the points on a sloped piece.
    /// </summary>
    [Fact]
    public void SF07_AFlyClearsOrMeetsTheSpansOwnTop()
    {
        var catalog = Game;
        var park = Fenced(catalog.Parks[ParkId.Harbor]);
        var polygon = FieldBounds.Of(park, Rules.Default);
        foreach (var (bearing, top) in new[] { (-38.0, 16.0), (25.0, 9.0) })
        {
            var fence = AtBatResolver.FenceAt(park, bearing);
            var (ix, iz) = BallFlight.GroundPoint(fence - 1, bearing);
            var (ox, oz) = BallFlight.GroundPoint(fence + 1, bearing);
            var crossing = polygon.Cross(ix, iz, ox, oz);
            Assert.NotNull(crossing);
            Near(top, crossing.Value.HeightFt, 1e-9);
            Near(top, AtBatResolver.FenceSpotAt(park, bearing).TopFt, 1e-9);
        }
        // A sloped piece: from 12 ft at 16° to 9 ft at 22°, the top in between is on the straight line.
        var mid = AtBatResolver.FenceAt(park, 19);
        var (mx, mz) = BallFlight.GroundPoint(mid - 1, 19);
        var (px, pz) = BallFlight.GroundPoint(mid + 1, 19);
        var sloped = polygon.Cross(mx, mz, px, pz)!.Value;
        Assert.NotNull(sloped.Segment.HeightBFt);
        Assert.InRange(sloped.HeightFt, 9 + 1e-6, 12 - 1e-6);
        Near(AtBatResolver.FenceSpotAt(park, 19).TopFt, sloped.HeightFt, 1e-9);
    }

    // ---------------------------------------------------------------------------------
    // SF-07 — the validator (FD-06-R1), by name
    // ---------------------------------------------------------------------------------

    /// <summary>The fixture as a park file's block.</summary>
    static JsonObject Block(Action<JsonArray>? change = null)
    {
        var points = new JsonArray();
        foreach (var p in Fixture)
        {
            var row = new JsonObject { ["bearingDeg"] = p.BearingDeg, ["fenceFrac"] = p.FenceFrac, ["heightFt"] = p.HeightFt };
            if (p.Material is not null) row["material"] = p.Material;
            points.Add(row);
        }
        change?.Invoke(points);
        return new JsonObject { ["points"] = points };
    }

    /// <summary>A valid block loads and the catalog park carries it, point for point.</summary>
    [Fact]
    public void SF07_AValidFenceBlockLoadsAndThePointsReachThePark()
    {
        using var shipped = new ContentFixture();
        shipped.ChangeObject("parks/harbor-diamond.json", json => json["fence"] = Block());
        Assert.Empty(ContentDataValidator.Validate(new DataRoot(shipped.Root)));
        var loaded = ContentCatalog.Load(new DataRoot(shipped.Root)).Parks[ParkId.Harbor];
        Assert.Equal(new ParkFence(Fixture), loaded.Fence);


        // The same authoring, a different field: a park with deeper posts stands the same points deeper (FD-12).
        var deeper = loaded with { CenterFenceFt = loaded.CenterFenceFt + 40 };
        Assert.Equal(loaded.Fence, deeper.Fence);
        Assert.NotEqual(AtBatResolver.FenceAt(loaded, 0), AtBatResolver.FenceAt(deeper, 0));
    }

    public static TheoryData<string, string[]> Refusals() => new()
    {
        { "overhang", ["fence.points[3] bearingDeg -35 is left of points[2]'s -29", "an overhang", "(FD-06-R1)"] },
        { "behind", ["fence.points[2] bearingDeg -31 is points[1]'s bearing", "a fence behind a fence", "(FD-06-R1)"] },
        { "left-pole", ["fence.points[0] bearingDeg must be -45, the left-field line", "got -31"] },
        { "right-pole", ["fence.points[9] bearingDeg must be 45, the right-field line", "got 34"] },
        { "one-point", ["fence.points must list at least 2 points", "got 1"] },
        { "lip", ["fence.points[4] stands", "ft from home", "inside the", "ft infield lip"] },
        { "chord", ["fence.points[0] to [1] runs", "ft from home at its nearest", "infield lip"] },
        { "height", ["fence.points[5] heightFt must stand over the 4.2 ft foul rail (D15); got 4"] },
        { "no-height", ["fence.points[6] heightFt must be a finite number of feet; got none"] },
        { "material", ["fence.points[6] material must be a wall material with a row in", "walls.json", "the library is [padded, glass]; got 'brick'"] },
        { "last-material", ["fence.points[10] material names the span from a point to the next one", "got 'padded'"] },
        { "frac", ["fence.points[1] fenceFrac must be greater than 0", "got 0"] },
    };

    static void Break(string what, JsonArray points)
    {
        switch (what)
        {
            case "overhang": points[3]!["bearingDeg"] = -35; break;
            case "behind": points[2]!["bearingDeg"] = -31; break;
            case "left-pole": points.RemoveAt(0); break;
            case "right-pole": points.RemoveAt(points.Count - 1); break;
            case "one-point": while (points.Count > 1) points.RemoveAt(1); break;
            case "lip": points[4]!["fenceFrac"] = 0.3; break;
            case "chord":
                // Two points, both past the lip, whose straight wall dips inside it at centre.
                while (points.Count > 2) points.RemoveAt(1);
                points[0]!["fenceFrac"] = 0.62;
                points[1]!["fenceFrac"] = 0.62;
                break;
            case "height": points[5]!["heightFt"] = 4; break;
            case "no-height": points[6]!.AsObject().Remove("heightFt"); break;
            case "material": points[6]!["material"] = "brick"; break;
            case "last-material": points[^1]!["material"] = "padded"; break;
            case "frac": points[1]!["fenceFrac"] = 0; break;
            default: throw new ArgumentOutOfRangeException(nameof(what), what, null);
        }
    }

    /// <summary>
    /// <c>SF-07</c>, FD-06-R1: an overhang, a fence behind a fence, a missing pole point (either end), a point inside the lip,
    /// a straight span that dips inside it, a height under the rail or missing, an unknown material, a material on the
    /// right-field pole's point and a zero fraction are each refused — by park, index and reason — against the park file
    /// the block was written in, and each stops the catalog load. Each case is the only refusal its broken block earns.
    /// </summary>
    [Theory]
    [MemberData(nameof(Refusals))]
    public void SF07_AnOverhangAFenceBehindAFenceAMissingPoleAPointInsideTheLipAHeightUnderTheRailAndAnUnknownMaterialAreRefusedByName(
        string what, string[] says)
    {
        using var shipped = new ContentFixture();
        shipped.ChangeObject("parks/harbor-diamond.json", json => json["fence"] = Block(points => Break(what, points)));
        var onShipped = Assert.Single(ContentDataValidator.Validate(new DataRoot(shipped.Root)), e => e.Contains("fence.points", StringComparison.Ordinal));
        Assert.StartsWith(shipped.Path("parks/harbor-diamond.json") + ": park 'harbor-diamond' fence.points", onShipped, StringComparison.Ordinal);
        foreach (var part in says) Assert.Contains(part, onShipped, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(new DataRoot(shipped.Root)));
    }

    /// <summary>The block is inside the strict park schema (#820, FR-03): a key it does not declare is named, in the block and in a point.</summary>
    [Fact]
    public void SF07_AnUnknownKeyInTheFenceBlockOrInAPointIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("parks/harbor-diamond.json", json =>
        {
            var block = Block(points => points[2]!["climbable"] = true);
            block["heightFt"] = 12;
            json["fence"] = block;
        });
        var errors = ContentDataValidator.Validate(new DataRoot(fixture.Root));
        Assert.Contains(errors, e => e.Contains("fence.heightFt is not a key this file declares", StringComparison.Ordinal)
                                     && e.Contains("[points]", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("fence.points[2].climbable is not a key this file declares", StringComparison.Ordinal)
                                     && e.Contains("[bearingDeg, fenceFrac, heightFt, material]", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // SF-12 — each span's own material
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-12</c>, the per-span half. Each fence piece carries its own span's material and the carom asks the
    /// library for that material's row, one span at a time: on a fixture whose left half names <c>padded</c> and whose
    /// right half names a material the library has no row for (<c>boards</c> — built in code, because the validator
    /// refuses it in a file), a ball rolled into the left half caroms off <c>padded</c>'s row, and the same roll into the
    /// right half stops and names <c>'boards'</c> and <c>walls.json</c> (<c>SF-03</c>) instead of borrowing its neighbour's
    /// row. The foul rail stays <c>padded</c>.
    ///
    /// <para>
    /// <b>What this row cannot show yet.</b> The library is closed and has one row (FR-02): a second material is a named
    /// row in <c>walls.json</c> and an id in <see cref="WallMaterial"/>, and choosing it is content — F9-a's glass boards —
    /// so a fixture cannot add one. Two spans caroming off two <em>different</em> rows lands with that row; the read it
    /// will use is the one proved here.
    /// </para>
    /// </summary>
    [Fact]
    public void SF12_EachSpanAsksTheLibraryForItsOwnMaterialsRow()
    {
        FencePoint[] halves = [new(-45, 1.0, 12, WallMaterial.Padded), new(0, 1.0, 12, "boards"), new(45, 1.0, 12)];
        var catalog = Game;
        var rules = catalog.Rules;
        var park = Fenced(catalog.Parks[ParkId.Harbor], halves);
        var polygon = FieldBounds.Of(park, Rules.Default);
        foreach (var s in polygon.Segments)
        {
            var expected = s.Kind == FieldBounds.WallKind.FoulWall ? WallMaterial.Padded
                : FieldBounds.SprayDeg((s.Ax + s.Bx) / 2, (s.Az + s.Bz) / 2) < 0 ? WallMaterial.Padded : "boards";
            Assert.Equal(expected, WallMaterial.OfSegment(s));
        }

        var padded = Roll(park, rules, -20);
        Assert.Contains(padded, s => s.Event == SampleEvent.Wall);
        var thrown = Assert.Throws<ArgumentException>(() => Roll(park, rules, 20));
        Assert.Contains("'boards' is not a wall material with a row in rules/walls.json", thrown.Message, StringComparison.Ordinal);
        Assert.False(rules.Walls.Has("boards"));

        static IReadOnlyList<Sample> Roll(Park park, RulesTable rules, double bearing)
        {
            var (x, z) = BallFlight.GroundPoint(AtBatResolver.FenceAt(park, bearing) - 6, bearing);
            var (ux, uz) = BallFlight.GroundPoint(40, bearing);
            return BallFlight.Continue([], 0, x, 0, z, ux, 0, uz, 0, 90, park, rules);
        }
    }

    // ---------------------------------------------------------------------------------
    // The cache key carries the fence, by value
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Map finding 14: the polygon and the drawn loop are cached on every input, the fence included, by value. A park with
    /// the same id and posts but a polyline is a different field; the same points in a new list are the same field; and
    /// asking for the fenced one does not change the catalog park's.
    /// </summary>
    [Fact]
    public void ThePolygonAndTheLoopAreCachedOnTheFenceByValue()
    {
        var arc = Game.Parks[ParkId.Harbor];
        var fenced = arc with { Fence = new ParkFence(Fixture.ToList()) };
        var again = arc with { Fence = new ParkFence(Fixture.Select(p => p with { }).ToArray()) };
        Assert.Equal(arc.Id, fenced.Id);

        var plain = FieldBounds.Of(arc, Rules.Default);
        var polygon = FieldBounds.Of(fenced, Rules.Default);
        Assert.NotSame(plain, polygon);
        Assert.Same(polygon, FieldBounds.Of(again, Rules.Default));
        Assert.Same(plain, FieldBounds.Of(arc, Rules.Default));
        Assert.NotEqual(plain.RadiusAt(0), polygon.RadiusAt(0));

        var loop = HarborWall.Loop(fenced);
        Assert.NotSame(HarborWall.Loop(arc), loop);
        Assert.Same(loop, HarborWall.Loop(again));
        Assert.Equal(HarborWall.WrapSegs, HarborWall.Loop(arc).Length);

        // One more point is another field.
        var moved = arc with { Fence = new ParkFence(Fixture.Select((p, i) => i == 4 ? p with { FenceFrac = 0.95 } : p).ToList()) };
        Assert.NotSame(polygon, FieldBounds.Of(moved, Rules.Default));
    }

    // ---------------------------------------------------------------------------------

    /// <summary>The span holding a bearing, as the points say (not as the code under test finds it).</summary>
    static int SpanOf(Park park, double bearing)
    {
        for (var k = 0; k + 1 < Fixture.Length; k++)
            if (bearing >= Fixture[k].BearingDeg && bearing <= Fixture[k + 1].BearingDeg) return k;
        throw new ArgumentOutOfRangeException(nameof(bearing));
    }

    /// <summary>The top on span <paramref name="span"/>'s chord at (x, z): straight between its two points' heights.</summary>
    static double TopOnChord(Park park, int span, double x, double z)
    {
        var a = PointAt(park, Fixture[span]);
        var b = PointAt(park, Fixture[span + 1]);
        var along = Diamond.Dist(a.X, a.Z, x, z) / Diamond.Dist(a.X, a.Z, b.X, b.Z);
        return Fixture[span].HeightFt + (Fixture[span + 1].HeightFt - Fixture[span].HeightFt) * along;
    }

    static double OffLine((double X, double Z) a, (double X, double Z) b, double x, double z)
    {
        var ex = b.X - a.X;
        var ez = b.Z - a.Z;
        return Math.Abs((x - a.X) * ez - (z - a.Z) * ex) / Math.Sqrt(ex * ex + ez * ez);
    }

    /// <summary>Where the ray from home at <paramref name="bearing"/> meets the chord a→b, or null.</summary>
    static double? RayMeetsChord((double X, double Z) a, (double X, double Z) b, double bearing)
    {
        var (dx, dz) = BallFlight.GroundPoint(1, bearing);
        var ex = b.X - a.X;
        var ez = b.Z - a.Z;
        var denom = dx * ez - dz * ex;
        if (Math.Abs(denom) < 1e-12) return null;
        var t = (a.X * ez - a.Z * ex) / denom;
        var u = (a.X * dz - a.Z * dx) / denom;
        return t > 0 && u >= -1e-12 && u <= 1 + 1e-12 ? t : null;
    }

    static (double X, double Z) Velocity(IReadOnlyList<Sample> path, int i, double dt) =>
        ((path[i].X - path[i - 1].X) / dt, (path[i].Z - path[i - 1].Z) / dt);

    /// <summary>Within a tolerance, not "to N places": rounding each side can split two values 5e-14 apart.</summary>
    static void Near(double expected, double actual, double tolerance) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected:R}, got {actual:R} (tolerance {tolerance})");

    static void Bits(string what, double expected, double actual) =>
        Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual),
            $"{what}: expected {expected:R}, got {actual:R}");

    // ---------------------------------------------------------------------------------
    // The code before the polyline, kept as the expectation
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <see cref="AtBatResolver.FenceAt"/>, <see cref="FieldBounds"/>'s polygon and <see cref="HarborWall"/>'s loop as they
    /// were at <c>2f1d56ba</c>, before the polyline, verbatim but for the names. The edge (<see cref="ParkBoundary"/>) is
    /// data since F2-a and is handed in; it is not this child's to move.
    /// </summary>
    static class Old
    {
        const double FoulLineDeg = 45;
        const double RoundFenceMinFt = 50;

        public static double FenceAt(Park park, double sprayDeg)
        {
            var t = Math.Clamp((sprayDeg + FoulLineDeg) / (FoulLineDeg * 2), 0, 1);
            var spray = -FoulLineDeg + t * 2 * FoulLineDeg;
            var round = RoundFence(park, spray);
            if (round > RoundFenceMinFt) return round;
            if (t < 0.5)
                return Lerp(park.LeftFenceFt, park.CenterFenceFt, t * 2);
            return Lerp(park.CenterFenceFt, park.RightFenceFt, (t - 0.5) * 2);
        }

        static double RoundFence(Park park, double sprayDeg)
        {
            var lf = Post(park.LeftFenceFt, -FoulLineDeg);
            var cf = Post(park.CenterFenceFt, 0);
            var rf = Post(park.RightFenceFt, FoulLineDeg);
            var ax = lf.X;
            var az = lf.Z;
            var bx = cf.X;
            var bz = cf.Z;
            var cx = rf.X;
            var cz = rf.Z;
            var d = 2 * (ax * (bz - cz) + bx * (cz - az) + cx * (az - bz));
            if (Math.Abs(d) < 1e-6) return 0;
            var a2 = ax * ax + az * az;
            var b2 = bx * bx + bz * bz;
            var c2 = cx * cx + cz * cz;
            var ux = (a2 * (bz - cz) + b2 * (cz - az) + c2 * (az - bz)) / d;
            var uz = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;
            var r2 = (ux - bx) * (ux - bx) + (uz - bz) * (uz - bz);
            var rad = sprayDeg * Math.PI / 180.0;
            var sx = Math.Sin(rad);
            var sz = Math.Cos(rad);
            var b = sx * ux + sz * uz;
            var disc = b * b - (ux * ux + uz * uz - r2);
            if (disc < 0) return 0;
            var root = Math.Sqrt(disc);
            var far = Math.Max(b + root, b - root);
            return far > RoundFenceMinFt ? far : 0;
        }

        static (double X, double Z) Post(double fenceFt, double sprayDeg)
        {
            var rad = sprayDeg * Math.PI / 180.0;
            return (Math.Sin(rad) * fenceFt, Math.Cos(rad) * fenceFt);
        }

        static double Lerp(double a, double b, double t) => a + (b - a) * t;

        public static IReadOnlyList<FieldBounds.WallSegment> Polygon(Park park, ParkBoundary bounds)
        {
            const int fenceSegs = 48;
            const int foulSegs = 20;
            const int homeSegs = 16;
            var pts = new List<(double X, double Z)>(fenceSegs + 2 * foulSegs + homeSegs + 4);
            for (var i = 0; i <= fenceSegs; i++)
            {
                var spray = -FoulLineDeg + 2 * FoulLineDeg * i / fenceSegs;
                pts.Add(BallFlight.GroundPoint(FenceAt(park, spray), spray));
            }
            var right = FoulSide(park, 1, bounds);
            pts.AddRange(right);
            var rightHome = right[^1];
            var r = Math.Sqrt(rightHome.X * rightHome.X + rightHome.Z * rightHome.Z);
            var a0 = Math.Atan2(rightHome.X, rightHome.Z);
            for (var i = 1; i < homeSegs; i++)
            {
                var a = a0 + (2 * Math.PI - 2 * a0) * i / homeSegs;
                pts.Add((Math.Sin(a) * r, Math.Cos(a) * r));
            }
            var left = FoulSide(park, -1, bounds);
            for (var i = left.Count - 1; i >= 0; i--)
                pts.Add(left[i]);

            var segs = new List<FieldBounds.WallSegment>(pts.Count);
            for (var i = 0; i < pts.Count; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Count];
                var fair = i < fenceSegs;
                var kind = fair ? FieldBounds.WallKind.FairFence : FieldBounds.WallKind.FoulWall;
                var height = fair ? park.FenceHeightFt : bounds.RailTopFt;
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

        static List<(double X, double Z)> FoulSide(Park park, int sign, ParkBoundary bounds)
        {
            var poleR = FenceAt(park, sign * FoulLineDeg);
            var side = new List<(double X, double Z)>(22);
            for (var i = 1; i <= 20; i++)
                side.Add(bounds.RailPoint(sign, poleR * (1 - i / (double)20), poleR));
            return side;
        }

        public static (double X, double Z)[] Loop(Park park, ParkBoundary bounds)
        {
            var right = HalfLoop(park, 1, bounds);
            var left = HalfLoop(park, -1, bounds);
            var pts = new List<(double X, double Z)>(HarborWall.WrapSegs);
            pts.AddRange(right);
            for (var i = left.Count - 2; i >= 1; i--)
                pts.Add(left[i]);
            return pts.ToArray();
        }

        static List<(double X, double Z)> HalfLoop(Park park, int sign, ParkBoundary bounds)
        {
            const int outfieldSegs = 48;
            const int foulSegs = 20;
            const int homeSegs = 16;
            var half = new List<(double X, double Z)>();
            for (var i = outfieldSegs / 2; i <= outfieldSegs; i++)
            {
                var spray = -FoulLineDeg + 2 * FoulLineDeg * i / outfieldSegs;
                var s = sign * spray;
                var fence = FenceAt(park, s);
                var rad = s * Math.PI / 180.0;
                half.Add((Math.Sin(rad) * fence, Math.Cos(rad) * fence));
            }
            var poleR = FenceAt(park, sign * FoulLineDeg);
            var alongs = new List<double>(foulSegs + 2);
            for (var i = 1; i <= foulSegs; i++)
                alongs.Add(poleR * (1 - i / (double)foulSegs));
            alongs.Add(HarborDugout.AlongBag);
            alongs.Add(HarborDugout.AlongHome);
            alongs.Sort((a, b) => b.CompareTo(a));
            foreach (var along in alongs)
                half.Add(bounds.RailPoint(sign, along, poleR));
            var home = half[^1];
            var r = Math.Sqrt(home.X * home.X + home.Z * home.Z);
            var a0 = Math.Atan2(home.X, home.Z);
            var a1 = sign * Math.PI;
            for (var i = 1; i <= homeSegs / 2; i++)
            {
                var t = i / (double)(homeSegs / 2);
                var a = a0 + (a1 - a0) * t;
                half.Add((Math.Sin(a) * r, Math.Cos(a) * r));
            }
            return half;
        }
    }
}
