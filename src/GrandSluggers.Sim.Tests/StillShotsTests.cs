using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F7-b1 (#882; FD-17 C, FD-16, FR-14, FR-05, FR-04): the named still shots <c>pole-left</c> and <c>pole-right</c>. Each
/// frames that side's foul pole with the last span of the fence and the last stretch of the rail either side of it, from
/// the fair side of the line at about head height, so a look gate at the poles (F2-b2, FD-06-R2) can be judged.
///
/// <para>
/// The pose is computed from the geometry owner for the park being captured (<see cref="StillShots.Frame"/>), so every
/// row here runs every park in <see cref="ContentCatalog.ParkPickOrder"/> on the shipped root and on <c>trials/c80</c>,
/// both loaded by hand, both sides, plus a polyline fixture whose pole stands on a short, tall porch. The framing
/// numbers are the <c>parkShots</c> rows of <c>data/feel/shots.json</c>; nothing here restates them.
/// </para>
///
/// <para>
/// Tolerances, stated once. The target is the pole to <see cref="ExactFt"/>: it is the pole's own coordinates, so any
/// slack would hide a second source. The frame is judged at the default still's aspect (1920 × 1080) inside a
/// <see cref="SafeFrame"/> border, so a point on the very edge of the picture does not count as framed.
/// </para>
/// </summary>
public sealed class StillShotsTests
{
    const double ExactFt = 1e-9;
    /// <summary>Arithmetic on the pose (a subtraction and a dot product of its parts), not a second source.</summary>
    const double ArithmeticFt = 1e-6;
    /// <summary>A framed point sits inside the middle 90 % of the picture's width and height.</summary>
    const double SafeFrame = 0.9;

    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    static string TrialDir =>
        Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "c80"));

    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped, TrialDir));

    static IEnumerable<ContentCatalog> BothRoots => [Shipped, Trial];

    static readonly string[] PoleIds = ["pole-left", "pole-right"];

    /// <summary>The picture the default request captures: the still's own width over its height.</summary>
    static double Aspect
    {
        get
        {
            var req = StillRequest.Parse("{}");
            return req.ResolvedWidth() / (double)req.ResolvedHeight();
        }
    }

    static IEnumerable<ParkShot> PoleRows(ContentCatalog catalog) =>
        PoleIds.Select(id => catalog.Shots.ParkById[id]);

    [Fact]
    public void EveryPoleShotFramesItsPoleAtEveryParkOnBothRootsAndBothSides()
    {
        var framed = 0;
        foreach (var catalog in BothRoots)
        {
            var bounds = ParkBoundary.From(catalog.Rules.Boundary);
            foreach (var id in catalog.ParkPickOrder)
            foreach (var row in PoleRows(catalog))
            {
                AssertFramesThePole(catalog.MustPark(id), row, bounds, $"{catalog.Root.Provenance} {id} {row.Id}");
                framed++;
            }
        }
        Assert.Equal(2 * 2 * Shipped.ParkPickOrder.Count, framed);
    }

    /// <summary>
    /// A lopsided park's two poles are different distances, so its two shots stand in different places — the same
    /// distance from their own pole. Nothing in the pose is a literal that happens to fit Harbor.
    /// </summary>
    [Fact]
    public void ALopsidedParksTwoShotsStandTheSameWayFromTwoDifferentPoles()
    {
        foreach (var catalog in BothRoots)
        {
            var bounds = ParkBoundary.From(catalog.Rules.Boundary);
            var lopsided = catalog.ParkPickOrder.Select(catalog.MustPark)
                .Where(p => p.LeftFenceFt != p.RightFenceFt).ToList();
            Assert.NotEmpty(lopsided);
            foreach (var park in lopsided)
            {
                var left = StillShots.Frame(catalog.Shots.ParkById["pole-left"], park, bounds);
                var right = StillShots.Frame(catalog.Shots.ParkById["pole-right"], park, bounds);
                var dl = Math.Sqrt(left.Pos.X * left.Pos.X + left.Pos.Z * left.Pos.Z);
                var dr = Math.Sqrt(right.Pos.X * right.Pos.X + right.Pos.Z * right.Pos.Z);
                // The shot on the deeper pole stands deeper, by the difference between the poles.
                var poleGap = AtBatResolver.FenceAt(park, 45) - AtBatResolver.FenceAt(park, -45);
                Assert.True(Math.Sign(dr - dl) == Math.Sign(poleGap), $"{park.Id}: the shots do not follow the poles");
                Assert.Equal(Reach(left, park, -1), Reach(right, park, 1), 9);
            }
        }
    }

    /// <summary>
    /// A park whose fence names points (F2-c): a short, tall porch at the left pole and a deeper, lower corner at the
    /// right. The pole moves in with the fence and the look rises and falls with the fence's own top there.
    /// </summary>
    [Fact]
    public void APolylineFenceMovesThePoleAndTheLookWithIt()
    {
        FencePoint[] points =
        [
            new(-45, 0.9, 18),
            new(-38, 0.9, 18),
            new(-34, 1.0, 12),
            new(34, 1.0, 12),
            new(40, 1.06, 9),
            new(45, 1.06, 9)
        ];
        foreach (var catalog in BothRoots)
        {
            var bounds = ParkBoundary.From(catalog.Rules.Boundary);
            foreach (var id in catalog.ParkPickOrder)
            {
                var plain = catalog.MustPark(id);
                var fenced = plain with { Id = plain.Id + "-f7b1", Fence = new ParkFence(points) };
                foreach (var row in PoleRows(catalog))
                {
                    var what = $"{catalog.Root.Provenance} {fenced.Id} {row.Id}";
                    AssertFramesThePole(fenced, row, bounds, what);
                    var before = StillShots.Frame(row, plain, bounds);
                    var after = StillShots.Frame(row, fenced, bounds);
                    Assert.NotEqual(before.Target, after.Target);
                    var top = row.Side < 0 ? 18 : 9;
                    Assert.True(Math.Abs(after.Target.Y - (bounds.RailTopFt + top) * 0.5) <= ExactFt, what);
                }
            }
        }
    }

    [Fact]
    public void TheParkShotsAreTheStillsTheRequestAcceptsAndNoneIsAFixedPose()
    {
        foreach (var catalog in BothRoots)
        {
            Assert.Equal(PoleIds, catalog.Shots.ParkById.Keys.OrderBy(k => k, StringComparer.Ordinal));
            foreach (var (id, row) in catalog.Shots.ParkById)
            {
                Assert.Equal(id, row.Id);
                Assert.Equal(StillShots.PoleFrame, row.Frame);
                Assert.Contains(id, StillRequest.AllowedShots);
                Assert.DoesNotContain(id, StillRequest.DefaultShots);
                Assert.False(catalog.Shots.TryGet(id, out _), id + " has a fixed pose");
            }
            Assert.Equal(-1, catalog.Shots.ParkById["pole-left"].Side);
            Assert.Equal(1, catalog.Shots.ParkById["pole-right"].Side);
        }
        // Every pole the request accepts has a row: a request for one cannot reach a capture with no pose.
        foreach (var id in StillRequest.AllowedShots.Where(s => s.StartsWith("pole-", StringComparison.Ordinal)))
            Assert.True(Shipped.Shots.TryGetPark(id, out _), id);
    }

    public static TheoryData<string, string, string> BadRows => new()
    {
        { "back", """{"backFt":null}""", "needs backFt" },
        { "fair", """{"fairFt":-4}""", "fairFt must be a finite number over 0" },
        { "eye", """{"eyeFt":40}""", "eyeFt must be at most" },
        { "narrow", """{"fov":5}""", "fov must be in" },
        { "wide", """{"fov":120}""", "fov must be in" },
        { "hold", """{"holdFt":0}""", "holdFt must be a finite number over 0" },
        { "side", """{"side":"up"}""", "side must be 'left' or 'right'" },
        { "frame", """{"frame":"wall"}""", "frame must be 'pole'" },
        { "id", """{"id":" "}""", "needs an id" },
        { "fixed", """{"id":"field"}""", "'field' is already a shot" },
    };

    /// <summary>The validator refuses a bad row by file, row and reason, and the catalog does not load around it.</summary>
    [Theory]
    [MemberData(nameof(BadRows))]
    public void TheValidatorRefusesABadRowByName(string label, string patch, string reason)
    {
        _ = label;
        var file = JsonNode.Parse(File.ReadAllText(Shipped.Root.Resolve("feel", "shots.json")))!.AsObject();
        var row = file["parkShots"]!.AsArray()[0]!.AsObject();
        // A null in the patch removes the key; anything else replaces it.
        foreach (var (key, value) in JsonNode.Parse(patch)!.AsObject())
        {
            if (value is null) row.Remove(key);
            else row[key] = value.DeepClone();
        }
        var ex = Assert.Throws<InvalidDataException>(() => Load(file));
        Assert.Contains(reason, ex.Message);
        Assert.Contains("parkShots[0]", ex.Message);
    }

    [Fact]
    public void TwoRowsWithOneIdAreRefusedAndTheShippedFileLoadsAsIs()
    {
        var file = JsonNode.Parse(File.ReadAllText(Shipped.Root.Resolve("feel", "shots.json")))!.AsObject();
        var shots = Load(file);
        Assert.Equal(PoleIds.Length, shots.ParkById.Count);

        var rows = file["parkShots"]!.AsArray();
        rows[1]!["id"] = "POLE-LEFT";
        var ex = Assert.Throws<InvalidDataException>(() => Load(file));
        Assert.Contains("parkShots[1] 'POLE-LEFT' is already a shot", ex.Message);
    }

    /// <summary>A pose that would stand behind the plate is refused by name, not folded onto the line.</summary>
    [Fact]
    public void AShotThatWouldStandBehindThePlateIsRefused()
    {
        var park = Shipped.MustPark(ExhibitionPick.DefaultPark);
        var row = Shipped.Shots.ParkById["pole-right"] with { BackFt = park.RightFenceFt + 10 };
        var ex = Assert.Throws<InvalidDataException>(() =>
            StillShots.Frame(row, park, ParkBoundary.From(Shipped.Rules.Boundary)));
        Assert.Contains("behind the plate", ex.Message);
    }

    static CameraShots Load(JsonObject file)
    {
        var root = Path.Combine(Path.GetTempPath(), "gs-f7b1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "feel"));
        try
        {
            File.WriteAllText(Path.Combine(root, "feel", "shots.json"), file.ToJsonString());
            return CameraShots.Load(new DataRoot(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>How far the camera stands from its pole on the ground.</summary>
    static double Reach(CameraShot pose, Park park, int sign)
    {
        var pole = ParkDiamond.FoulPole(park, sign);
        return Math.Sqrt(Sq(pose.Pos.X - pole.X) + Sq(pose.Pos.Z - pole.Z));
    }

    static double Sq(double v) => v * v;

    static void AssertFramesThePole(Park park, ParkShot row, ParkBoundary bounds, string what)
    {
        var sign = row.Side;
        var pose = StillShots.Frame(row, park, bounds);
        Assert.Equal(row.Id, pose.Id);
        Assert.Equal(row.Fov, pose.Fov);
        Assert.Equal(0, pose.Blend);

        // The target is the pole, at the middle of the step from the rail's top to the fence's top there.
        var pole = ParkDiamond.FoulPole(park, sign);
        var poleFt = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
        var fenceTop = AtBatResolver.FenceSpotAt(park, sign * AtBatResolver.FoulLineDeg).TopFt;
        Assert.True(Math.Abs(pose.Target.X - pole.X) <= ExactFt && Math.Abs(pose.Target.Z - pole.Z) <= ExactFt,
            $"{what}: target ({pose.Target.X}, {pose.Target.Z}) is not the pole ({pole.X}, {pole.Z})");
        Assert.True(Math.Abs(pose.Target.Y - (bounds.RailTopFt + fenceTop) * 0.5) <= ExactFt, $"{what}: look height");
        Assert.True(pose.Target.Y > bounds.RailTopFt && pose.Target.Y < fenceTop, $"{what}: the look is not on the step");

        // The camera: back along the line from the pole, on the fair side of it, inside the fence, at the row's eye.
        var along = (pole.X / poleFt, pole.Z / poleFt);
        var fair = ParkDiamond.FairInward(sign);
        var intoFair = pose.Pos.X * fair.X + pose.Pos.Z * fair.Z;
        var back = (pole.X - pose.Pos.X) * along.Item1 + (pole.Z - pose.Pos.Z) * along.Item2;
        Assert.True(Math.Abs(intoFair - row.FairFt) <= ArithmeticFt, $"{what}: {intoFair} ft off the line, not {row.FairFt}");
        Assert.True(intoFair > 0, $"{what}: the camera is in foul territory");
        Assert.True(Math.Abs(back - row.BackFt) <= ArithmeticFt, $"{what}: {back} ft back from the pole, not {row.BackFt}");
        Assert.True(back > 0 && back < poleFt, $"{what}: the camera is not between the plate and the pole");
        var bearing = Math.Atan2(pose.Pos.X, pose.Pos.Z) * 180 / Math.PI;
        Assert.True(Math.Abs(bearing) < AtBatResolver.FoulLineDeg, $"{what}: bearing {bearing} is foul");
        var reach = Math.Sqrt(pose.Pos.X * pose.Pos.X + pose.Pos.Z * pose.Pos.Z);
        var fenceThere = AtBatResolver.FenceAt(park, bearing);
        Assert.True(reach < fenceThere, $"{what}: the camera stands {reach} ft out, the fence is {fenceThere}");
        Assert.Equal(row.EyeFt, pose.Pos.Y);

        // The picture: the pole from the grass to the fence's top, the fence holdFt along from the pole on one side and
        // the rail holdFt back along the line on the other, each inside the safe frame.
        var fence = FencePointFrom(park, sign, row.HoldFt);
        var fenceSpot = AtBatResolver.FenceSpotAt(park, fence.Bearing);
        var rail = bounds.RailPoint(sign, poleFt - row.HoldFt, poleFt);
        var marks = new (string Name, Vec3 At)[]
        {
            ("pole at the grass", new Vec3(pole.X, 0, pole.Z)),
            ("pole at the fence top", new Vec3(pole.X, fenceTop, pole.Z)),
            ("fence at the grass", new Vec3(fence.X, 0, fence.Z)),
            ("fence top", new Vec3(fence.X, fenceSpot.TopFt, fence.Z)),
            ("rail at the grass", new Vec3(rail.X, 0, rail.Z)),
            ("rail top", new Vec3(rail.X, bounds.RailTopFt, rail.Z))
        };
        foreach (var (name, at) in marks)
        {
            var (x, y, depth) = StillShots.Project(pose, at, Aspect);
            Assert.True(depth > 0 && Math.Abs(x) <= SafeFrame && Math.Abs(y) <= SafeFrame,
                $"{what}: {name} projects to ({x:0.###}, {y:0.###}) at depth {depth:0.#}, outside ±{SafeFrame}");
        }

        // The pole is the middle of the picture, the fence runs off to one side of it and the rail to the other.
        var poleAt = StillShots.Project(pose, new Vec3(pole.X, pose.Target.Y, pole.Z), Aspect);
        Assert.True(Math.Abs(poleAt.X) <= 1e-9 && Math.Abs(poleAt.Y) <= 1e-9, $"{what}: the pole is not the centre");
        var fenceX = StillShots.Project(pose, new Vec3(fence.X, fenceSpot.TopFt, fence.Z), Aspect).X;
        var railX = StillShots.Project(pose, new Vec3(rail.X, bounds.RailTopFt, rail.Z), Aspect).X;
        Assert.True(fenceX * railX < 0, $"{what}: fence ({fenceX:0.###}) and rail ({railX:0.###}) are on one side of the pole");
        // Right field: the fence runs left toward centre and the rail right into foul; left field is the mirror.
        Assert.Equal(-sign, Math.Sign(fenceX));
    }

    /// <summary>The first point on the fence at least <paramref name="holdFt"/> from the pole, walked in from the line.</summary>
    static (double X, double Z, double Bearing) FencePointFrom(Park park, int sign, double holdFt)
    {
        var pole = ParkDiamond.FoulPole(park, sign);
        for (var step = 1; step <= 45 * 100; step++)
        {
            var bearing = sign * (AtBatResolver.FoulLineDeg - step * 0.01);
            var r = AtBatResolver.FenceAt(park, bearing);
            var rad = bearing * Math.PI / 180;
            var (x, z) = (Math.Sin(rad) * r, Math.Cos(rad) * r);
            if (Math.Sqrt(Sq(x - pole.X) + Sq(z - pole.Z)) >= holdFt) return (x, z, bearing);
        }
        throw new InvalidOperationException($"{park.Id}: no fence point {holdFt} ft from the pole");
    }
}
