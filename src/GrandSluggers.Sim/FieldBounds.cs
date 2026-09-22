namespace GrandSluggers.Sim;

/// <summary>
/// The field's boundary for a park (spec §6.1): the outfield fence between the poles — read through
/// <see cref="AtBatResolver.FenceSpotAt"/>, so the circle through the park's L / C / R posts at
/// <see cref="Park.FenceHeightFt"/>, or the park's own <see cref="Park.Fence"/> polyline with a top per
/// point and a wall material per span (F2-c) — and the foul wrap (hip rail along the lines, round
/// backstop) from the park's <see cref="ParkBoundary"/>. The flight clips against it, gloves plant
/// inside it, and fair / foul is the chalk wedge. Each park's JSON fences — not a Harbor 400.
///
/// <para>
/// No park's name appears here (#826, FR-05). The wrap was built from <c>HarborWall</c>'s literals
/// until F2-a: the rail, its flare and the backstop now come from <c>data/rules/boundary.json</c>
/// through <see cref="ParkBoundary"/>, at exactly the values that shipped, and the drawn kit reads
/// the same edge so the padding the player sees is the padding the ball meets.
/// </para>
/// </summary>
public static class FieldBounds
{
    public enum WallKind
    {
        /// <summary>Between the poles. Above the top is a home run (or a ground-rule double after a bounce).</summary>
        FairFence,
        /// <summary>Side rails and the backstop. Any touch is foul; above the top is the stands.</summary>
        FoulWall
    }

    /// <summary>
    /// One straight piece of the boundary, its top, its outward normal (toward the stands) and what it
    /// is made of.
    ///
    /// <para>
    /// <b>The top.</b> <see cref="HeightFt"/> is the top at A. A level piece — every foul piece, and every
    /// fence piece of a park with no polyline — has no <see cref="HeightBFt"/> and stands at
    /// <see cref="HeightFt"/> end to end, to the bit. A polyline span whose two points stand at different
    /// heights carries its top at B as well, and the top runs straight between them
    /// (<see cref="HeightAt"/>), which is the ramp the drawn wall draws between the same two vertices
    /// (D15 as amended by D21, <c>SF-05</c>).
    /// </para>
    ///
    /// <para>
    /// <b>The material</b> is the span's (FD-06, F2-c): <see cref="WallMaterial.OfSegment"/> reads it,
    /// and the carom takes that row. The foul rail and the fence of a park with no polyline are
    /// <see cref="WallMaterial.Padded"/>.
    /// </para>
    /// </summary>
    public sealed record WallSegment(
        double Ax, double Az, double Bx, double Bz, double HeightFt, WallKind Kind, double Nx, double Nz, double MinR, double MaxR,
        string Material = WallMaterial.Padded, double? HeightBFt = null)
    {
        /// <summary>The top at <paramref name="along"/> of the way from A (0) to B (1).</summary>
        public double HeightAt(double along) =>
            HeightBFt is { } b ? HeightFt + (b - HeightFt) * Math.Clamp(along, 0, 1) : HeightFt;
    }

    /// <summary>
    /// Where a step met a segment: <see cref="U"/> of the way along the step, <see cref="V"/> of the way
    /// from the segment's A to its B, at (<see cref="X"/>, <see cref="Z"/>).
    /// </summary>
    public readonly record struct Crossing(WallSegment Segment, double U, double X, double Z, double V)
    {
        /// <summary>The wall's top where the ball met it: the segment's top at <see cref="V"/> (<see cref="WallSegment.HeightAt"/>).</summary>
        public double HeightFt => Segment.HeightAt(V);
    }

    /// <summary>The closed polygon with heights, built once per park.</summary>
    public sealed class Boundary
    {
        internal Boundary(IReadOnlyList<WallSegment> segments, double fenceHeightFt)
        {
            Segments = segments;
            FenceHeightFt = fenceHeightFt;
        }

        public IReadOnlyList<WallSegment> Segments { get; }
        public double FenceHeightFt { get; }

        /// <summary>Inside the polygon (even-odd ray cast).</summary>
        public bool Contains(double x, double z)
        {
            var inside = false;
            foreach (var s in Segments)
            {
                if ((s.Az > z) == (s.Bz > z)) continue;
                var xAt = s.Ax + (z - s.Az) / (s.Bz - s.Az) * (s.Bx - s.Ax);
                if (x < xAt) inside = !inside;
            }
            return inside;
        }

        /// <summary>Distance from the plate to the boundary along this spray (0 out to CF, ±90 down the dugout lines, ±180 behind the plate).</summary>
        public double RadiusAt(double sprayDeg)
        {
            var rad = sprayDeg * Math.PI / 180.0;
            var dx = Math.Sin(rad);
            var dz = Math.Cos(rad);
            var best = double.MaxValue;
            foreach (var s in Segments)
            {
                var ex = s.Bx - s.Ax;
                var ez = s.Bz - s.Az;
                var denom = dx * ez - dz * ex;
                if (Math.Abs(denom) < 1e-9) continue;
                var t = (s.Ax * ez - s.Az * ex) / denom;
                var u = (s.Ax * dz - s.Az * dx) / denom;
                if (t > 0 && u >= -1e-9 && u <= 1 + 1e-9 && t < best) best = t;
            }
            return best == double.MaxValue ? 0 : best;
        }

        /// <summary>The first segment the step from (x0, z0) to (x1, z1) crosses going out, or null.</summary>
        public Crossing? Cross(double x0, double z0, double x1, double z1)
        {
            var r0 = Math.Sqrt(x0 * x0 + z0 * z0);
            var r1 = Math.Sqrt(x1 * x1 + z1 * z1);
            var lo = Math.Min(r0, r1) - 1;
            var hi = Math.Max(r0, r1) + 1;
            var dx = x1 - x0;
            var dz = z1 - z0;
            Crossing? best = null;
            foreach (var s in Segments)
            {
                if (s.MaxR < lo || s.MinR > hi) continue;
                // Only a step heading out through this wall counts; a carom starts on the inside.
                if (dx * s.Nx + dz * s.Nz <= 0) continue;
                var ex = s.Bx - s.Ax;
                var ez = s.Bz - s.Az;
                var denom = dx * ez - dz * ex;
                if (Math.Abs(denom) < 1e-12) continue;
                var fx = s.Ax - x0;
                var fz = s.Az - z0;
                var u = (fx * ez - fz * ex) / denom;
                var v = (fx * dz - fz * dx) / denom;
                if (u < 0 || u > 1 || v < -1e-9 || v > 1 + 1e-9) continue;
                if (best is null || u < best.Value.U)
                    best = new Crossing(s, u, x0 + dx * u, z0 + dz * u, v);
            }
            return best;
        }
    }

    /// <summary>Same pad as <see cref="FlyCatch.WallPlant"/> — warning track, not the crowd.</summary>
    public const double InsideFt = 8;

    /// <summary>The backstop wrap behind the plate (<see cref="ParkBoundary.BackstopZFt"/>).</summary>
    public static double BackstopZ => ParkBoundary.Default.BackstopZFt;

    /// <summary>Hip rail height along the foul wraps (<see cref="ParkBoundary.RailTopFt"/>).</summary>
    public static double FoulWallHeightFt => ParkBoundary.Default.RailTopFt;

    /// <summary>
    /// Everything the edge is built from, as one key (map finding 14): the three posts, the fence top
    /// the park carries, the park's <see cref="Park.Fence"/> polyline — by value, point for point — and
    /// the whole <see cref="ParkBoundary"/> it plays on. Before #826 the key was the posts alone, so a
    /// second foul area (F2-d) would have been served the first one's polygon, and nothing would have
    /// failed, because the posts would have matched; a polyline keyed on the posts would be the same
    /// bug again. The drawn loop's cache (<see cref="HarborWall"/>) uses this same key, so an input
    /// added here reaches both caches at once.
    /// </summary>
    internal readonly record struct EdgeKey(
        string Id, int Left, int Center, int Right, double Height, ParkFence? Fence, ParkBoundary Bounds)
    {
        public static EdgeKey Of(Park park, ParkBoundary bounds) =>
            new(park.Id, park.LeftFenceFt, park.CenterFenceFt, park.RightFenceFt, park.FenceHeightFt, park.Fence, bounds);
    }

    /// <summary>The polygon per park and edge, built once (<see cref="EdgeKey"/>).</summary>
    static readonly System.Collections.Concurrent.ConcurrentDictionary<EdgeKey, Boundary> Cache = new();

    public static Boundary Of(Park park) => Of(park, ParkBoundary.Default);

    public static Boundary Of(Park park, ParkBoundary bounds) =>
        Cache.GetOrAdd(EdgeKey.Of(park, bounds), k => Build(park, k.Bounds));

    /// <summary>Segments along the outfield arc between the poles (the chord sag is under 0.1 ft at these radii).</summary>
    public const int FenceSegs = 48;
    const int FoulSegs = 20;
    const int HomeSegs = 16;

    /// <summary>
    /// The bearings the fence between the poles is built on, left pole to right pole: the
    /// <see cref="FenceSegs"/> spray grid, and for a park with a <see cref="Park.Fence"/> polyline every
    /// point's bearing merged in, so every point is a vertex and no corner of the polyline is cut (a grid
    /// bearing between two points only divides a straight span). A park with no points is the grid
    /// alone, computed exactly as it always was, so its polygon and its drawn loop are unchanged vertex
    /// for vertex (<c>SF-06</c>). The drawn loop walks the same list (<see cref="HarborWall"/>).
    /// </summary>
    public static IReadOnlyList<double> FenceBearings(Park park)
    {
        var bearings = new List<double>(FenceSegs + 1 + (park.Fence?.Points.Count ?? 0));
        for (var i = 0; i <= FenceSegs; i++)
            bearings.Add(-AtBatResolver.FoulLineDeg + 2 * AtBatResolver.FoulLineDeg * i / FenceSegs);
        if (park.Fence is not { } fence) return bearings;
        foreach (var point in fence.Points)
            if (!bearings.Contains(point.BearingDeg))
                bearings.Add(point.BearingDeg);
        bearings.Sort();
        return bearings;
    }

    /// <summary>
    /// The polygon: the park's own fence from the left pole to the right, on
    /// <see cref="FenceBearings"/> (each vertex where <see cref="AtBatResolver.FenceSpotAt"/> puts the
    /// fence, each piece carrying the fence's top at both its ends and the material of the span it
    /// lies on), then the foul wrap (<see cref="ParkBoundary.RailPoint"/>: a rail parallel to the line
    /// that flares to the pole) and the round backstop, which are <see cref="WallMaterial.Padded"/> and
    /// stand at the rail's top.
    /// </summary>
    static Boundary Build(Park park, ParkBoundary bounds)
    {
        var bearings = FenceBearings(park);
        var fence = new FenceSpot[bearings.Count];
        var pts = new List<(double X, double Z)>(bearings.Count + 2 * FoulSegs + HomeSegs + 4);
        for (var i = 0; i < bearings.Count; i++)
        {
            var spray = bearings[i];
            fence[i] = AtBatResolver.FenceSpotAt(park, spray);
            pts.Add(BallFlight.GroundPoint(fence[i].DistanceFt, spray));
        }
        var right = FoulSide(park, 1, bounds);
        pts.AddRange(right);
        var rightHome = right[^1];
        var r = Math.Sqrt(rightHome.X * rightHome.X + rightHome.Z * rightHome.Z);
        var a0 = Math.Atan2(rightHome.X, rightHome.Z);
        for (var i = 1; i < HomeSegs; i++)
        {
            var a = a0 + (2 * Math.PI - 2 * a0) * i / HomeSegs;
            pts.Add((Math.Sin(a) * r, Math.Cos(a) * r));
        }
        var left = FoulSide(park, -1, bounds);
        for (var i = left.Count - 1; i >= 0; i--)
            pts.Add(left[i]);

        var segs = new List<WallSegment>(pts.Count);
        for (var i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            var fair = i < bearings.Count - 1;
            var kind = fair ? WallKind.FairFence : WallKind.FoulWall;
            var height = fair ? fence[i].TopFt : bounds.RailTopFt;
            // A level piece stays level to the bit; only a span whose ends differ carries its far top.
            double? heightB = fair && fence[i + 1].TopFt != height ? fence[i + 1].TopFt : null;
            var material = fair ? fence[i].Material : WallMaterial.Padded;
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
            segs.Add(new WallSegment(a.X, a.Z, b.X, b.Z, height, kind, nx, nz, Math.Min(ra, rb), Math.Max(ra, rb), material, heightB));
        }
        return new Boundary(segs, park.FenceHeightFt);
    }

    /// <summary>The foul wrap on one side, pole → home, on this side's own pole radius.</summary>
    static List<(double X, double Z)> FoulSide(Park park, int sign, ParkBoundary bounds)
    {
        var poleR = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
        var side = new List<(double X, double Z)>(FoulSegs + 2);
        for (var i = 1; i <= FoulSegs; i++)
            side.Add(bounds.RailPoint(sign, poleR * (1 - i / (double)FoulSegs), poleR));
        return side;
    }

    public static double SprayDeg(double x, double z) =>
        Math.Atan2(x, z) * (180.0 / Math.PI);

    public static double DistHome(double x, double z) =>
        Math.Sqrt(x * x + z * z);

    /// <summary>The chalk (§5.6): inside the ±<see cref="AtBatResolver.FoulLineDeg"/> wedge, lines included, in front of the plate.</summary>
    public static bool IsFair(double x, double z) =>
        z >= 0 && Math.Abs(SprayDeg(x, z)) <= AtBatResolver.FoulLineDeg + 1e-9;

    /// <summary>A grounder is judged where it passes first or third (§5.6): the bag circle.</summary>
    public static bool PastTheBags(double x, double z) =>
        DistHome(x, z) >= Diamond.Baseline;

    /// <summary>Inside the park boundary (fair or foul); the ball is still on the field.</summary>
    public static bool InPark(Park park, double x, double z) =>
        Of(park).Contains(x, z);

    /// <summary>Inside the glove's playable area (the boundary less the warning-track pad).</summary>
    public static bool Inside(Park park, double x, double z)
    {
        var c = Clamp(park, x, z);
        return Diamond.Dist(x, z, c.X, c.Z) < 0.6;
    }

    /// <summary>Clip a glove (or landing) onto the grass for this park: inside every wall by <see cref="InsideFt"/>.</summary>
    public static (double X, double Z) Clamp(Park park, double x, double z) => Clamp(park, x, z, InsideFt);

    public static (double X, double Z) Clamp(Park park, double x, double z, double insetFt)
    {
        var dist = DistHome(x, z);
        if (dist < 0.5) return (x, z);
        var max = Math.Max(12, Of(park).RadiusAt(SprayDeg(x, z)) - insetFt);
        if (dist <= max + 1e-6) return (x, z);
        var u = max / dist;
        return (x * u, z * u);
    }
}
