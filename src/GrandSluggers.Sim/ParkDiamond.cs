namespace GrandSluggers.Sim;

/// <summary>
/// Shared diamond ground: dirt paths, mound, striped grass, foul poles, warning track.
/// Harbor fills these now. Other parks reuse the same outlines when they get a kit —
/// fence distances come from <see cref="Park"/>, bags and the rubber from <see cref="Diamond"/>.
/// </summary>
public static class ParkDiamond
{
    /// <summary>
    /// Grass Y: L1 diamond around second/mound. Bags sit outside this, on the dirt.
    /// </summary>
    public const float InnerHalf = 48f;
    /// <summary>
    /// Euclidean offset of the grass Y. Corner radius equals this so paths and
    /// bag pads are one rounded diamond — not straight slabs butted to circles.
    /// </summary>
    public const float SkinWidth = 22f;
    public const float PathWidth = SkinWidth;
    public const int SkinArcSegs = 10;
    public const int SkinEdgeSegs = 6;
    /// <summary>Center of the dirt slab. Must sit above <see cref="GrassTop"/> so the ring reads at couch.</summary>
    public const float PathY = 0.26f;
    public const float PathThick = 0.24f;
    /// <summary>Dirt top minus grass top. A 0.05-ft lip z-fights and vanishes under the lawn.</summary>
    public const float PathLip = 0.18f;

    /// <summary>Packed dirt circle around each bag. Not an 11-ft cylinder.</summary>
    public const float BagDirtR = 5.2f;

    /// <summary>White bag edge. Square, diamond-aligned.</summary>
    public const float BagSize = 1.85f;
    public const float BagY = 0.22f;

    /// <summary>Packed dirt around the plate. Not a 34-ft oval.</summary>
    public const float HomePackedR = 16f;

    /// <summary>One smooth dirt hill. Not stacked cylinders.</summary>
    public const float MoundR = 9.2f;
    public const float MoundH = 0.98f;
    /// <summary>Flat crown the rubber sits on. Wider than the rubber, smaller than the hill.</summary>
    public const float MoundTableR = 2.2f;
    public const float RubberY = 1.02f;
    public const float RubberW = 1.7f;
    public const float RubberH = 0.07f;
    public const float RubberD = 0.42f;

    /// <summary>Dirt band inside the wall. Must stay past the infield lip.</summary>
    public const float TrackWidth = 15f;
    public const int TrackSegs = 36;
    public const float TrackY = 0.14f;
    public const float TrackThick = 0.22f;

    public const float PoleHeight = 52f;
    public const float PoleRadius = 0.85f;
    public const float PoleScreenH = 16f;
    public const float PoleScreenW = 7f;
    public const float PoleScreenY = 38f;

    /// <summary>Mow stripe width. Bands of constant X — home → CF, vertical in the overhead.</summary>
    public const float StripeWidth = 18f;
    public const float GrassY = 0.08f;
    public const float GrassThick = 0.12f;
    public const float GrassZ0 = -28f;
    /// <summary>Foul-territory grass past the 45° line.</summary>
    public const float FoulGrassFt = 36f;

    public static bool PathIsNotALake() =>
        InnerHalf > 40f && SkinWidth < 28f && InnerHalf > SkinWidth * 1.5f;

    public static bool BagIsABag() => BagSize < 2.2f && BagDirtR < 7f && BagDirtR > BagSize;

    public static bool HomePackedIsAPad() => HomePackedR < 34f;

    public static bool MoundIsAHill() =>
        MoundR > 7f && MoundR < 14f
        && MoundTableR > RubberW * 0.45f && MoundTableR < MoundR * 0.45f
        && MoundH > GrassTop + 0.5f
        && MoundH <= RubberY
        && RubberY - MoundH < 0.08f
        && RubberY > 0.8f && RubberY < 1.4f;

    public static bool StripeReadsAtCouch() => StripeWidth >= 12f && StripeWidth <= 28f;

    /// <summary>Corner radius equals skin width (Minkowski offset). Slabs+bigger circles gap.</summary>
    public static bool PathCornersAreRound() =>
        SkinWidth >= 16f && SkinWidth <= 26f && Math.Abs(PathWidth - SkinWidth) < 0.01f;

    public static float GrassTop => GrassY + GrassThick * 0.5f;
    public static float PathTop => PathY + PathThick * 0.5f;
    public static float PathBottom => PathY - PathThick * 0.5f;

    /// <summary>Dirt ring sits on the lawn, not in it. Grass is drawn first; dirt after.</summary>
    public static bool DirtClearsTheLawn() =>
        PathTop >= GrassTop + PathLip && PathBottom >= GrassTop - 0.02f && PathY > GrassY;

    public static float CenterZ => (float)(Diamond.Second.Z * 0.5);

    public static float DirtMaxX => InnerHalf + SkinWidth;
    public static float DirtMaxZ => CenterZ + InnerHalf + SkinWidth;

    /// <summary>L1 diamond of the grass Y. Vertices point at the bags.</summary>
    public static (double X, double Z)[] InnerVerts() =>
    [
        (0, CenterZ - InnerHalf),
        (InnerHalf, CenterZ),
        (0, CenterZ + InnerHalf),
        (-InnerHalf, CenterZ)
    ];

    /// <summary>
    /// Packed home, mound pad, and a constant-width rounded diamond around the Y.
    /// </summary>
    public static bool OnDirt(double x, double z)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return true;
        if (Dist(x, z, 0, Diamond.Mound) <= MoundR) return true;
        var l1 = Math.Abs(x) + Math.Abs(z - CenterZ);
        if (l1 <= InnerHalf) return false;
        return DistToInnerDiamond(x, z) <= SkinWidth;
    }

    /// <summary>Grass Y inside the diamond, not home dirt and not the mound pad.</summary>
    public static bool OnInfieldGrass(double x, double z)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return false;
        if (Dist(x, z, 0, Diamond.Mound) <= MoundR) return false;
        return Math.Abs(x) + Math.Abs(z - CenterZ) <= InnerHalf;
    }

    /// <summary>
    /// Outer edge of the dirt skin: offset inner diamond by <see cref="SkinWidth"/>,
    /// quarter-circles at the vertices. CCW from home.
    /// </summary>
    public static (double X, double Z)[] OuterVerts()
    {
        var inner = InnerVerts();
        var n = inner.Length;
        var pts = new List<(double X, double Z)>(n * (SkinEdgeSegs + SkinArcSegs));
        for (var i = 0; i < n; i++)
        {
            var a = inner[i];
            var b = inner[(i + 1) % n];
            var c = inner[(i + 2) % n];
            var n1 = Outward(a, b);
            var n2 = Outward(b, c);
            var ax = a.X + n1.X * SkinWidth;
            var az = a.Z + n1.Z * SkinWidth;
            var bx = b.X + n1.X * SkinWidth;
            var bz = b.Z + n1.Z * SkinWidth;
            for (var s = 0; s < SkinEdgeSegs; s++)
            {
                var t = s / (double)SkinEdgeSegs;
                pts.Add((ax + (bx - ax) * t, az + (bz - az) * t));
            }
            var a0 = Math.Atan2(n1.Z, n1.X);
            var a1 = Math.Atan2(n2.Z, n2.X);
            var da = a1 - a0;
            while (da > Math.PI) da -= 2 * Math.PI;
            while (da < -Math.PI) da += 2 * Math.PI;
            for (var s = 1; s <= SkinArcSegs; s++)
            {
                var t = s / (double)SkinArcSegs;
                var ang = a0 + da * t;
                pts.Add((b.X + Math.Cos(ang) * SkinWidth, b.Z + Math.Sin(ang) * SkinWidth));
            }
        }
        return pts.ToArray();
    }

    public static (double X, double Z) InnerOnRay(double x, double z)
    {
        var dx = x;
        var dz = z - CenterZ;
        var l1 = Math.Abs(dx) + Math.Abs(dz);
        if (l1 < 1e-6) return (0, CenterZ);
        var s = InnerHalf / l1;
        return (dx * s, CenterZ + dz * s);
    }

    static (double X, double Z) Outward((double X, double Z) a, (double X, double Z) b)
    {
        var dx = b.X - a.X;
        var dz = b.Z - a.Z;
        var len = Math.Sqrt(dx * dx + dz * dz);
        var nx = dz / len;
        var nz = -dx / len;
        var mx = (a.X + b.X) * 0.5;
        var mz = (a.Z + b.Z) * 0.5 - CenterZ;
        if (nx * mx + nz * mz < 0)
        {
            nx = -nx;
            nz = -nz;
        }
        return (nx, nz);
    }

    static double DistToInnerDiamond(double x, double z)
    {
        var v = InnerVerts();
        var best = double.MaxValue;
        for (var i = 0; i < v.Length; i++)
        {
            var a = v[i];
            var b = v[(i + 1) % v.Length];
            best = Math.Min(best, DistToSegment(x, z, a.X, a.Z, b.X, b.Z));
        }
        return best;
    }

    static double Dist(double x1, double z1, double x2, double z2)
    {
        var dx = x1 - x2;
        var dz = z1 - z2;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    static double DistToSegment(double x, double z, double ax, double az, double bx, double bz)
    {
        var dx = bx - ax;
        var dz = bz - az;
        var len2 = dx * dx + dz * dz;
        if (len2 < 1e-6) return Dist(x, z, ax, az);
        var t = Math.Clamp(((x - ax) * dx + (z - az) * dz) / len2, 0, 1);
        return Dist(x, z, ax + t * dx, az + t * dz);
    }

    /// <summary>Stripes are columns along CF (X bands), not rows along 1B–3B.</summary>
    public static bool StripesRunHomeToCf() => true;

    public static (double X, double Z) FoulPole(Park park, int sign)
    {
        var spray = Math.Sign(sign) * AtBatResolver.FoulLineDeg;
        var fence = AtBatResolver.FenceAt(park, spray);
        var rad = spray * Math.PI / 180.0;
        return (Math.Sin(rad) * fence, Math.Cos(rad) * fence);
    }

    public static double TrackMid(Park park, double sprayDeg) =>
        AtBatResolver.FenceAt(park, sprayDeg) - TrackWidth * 0.5;

    public static float GrassHalfWidth(float z) =>
        Math.Max(40f, Math.Abs(z) + FoulGrassFt);

    public static float GrassZ1(Park park) =>
        (float)park.CenterFenceFt - TrackWidth;

    public static bool TrackIsInsideTheWall(Park park) =>
        TrackWidth > 8f && TrackWidth < 24f
        && TrackMid(park, 0) > FieldingResolver.InfieldLipFt
        && TrackMid(park, 0) < park.CenterFenceFt;

    public static bool PoleIsOnTheFoulLine(Park park)
    {
        var (x, z) = FoulPole(park, 1);
        var spray = Math.Atan2(x, z) * (180.0 / Math.PI);
        return Math.Abs(Math.Abs(spray) - AtBatResolver.FoulLineDeg) < 0.6;
    }

    public static bool PoleSitsOnThatParkFence(Park park)
    {
        var (x, z) = FoulPole(park, 1);
        var dist = Diamond.Dist(0, 0, x, z);
        return Math.Abs(dist - park.RightFenceFt) < 1.5;
    }

    public static bool LawnRespectsPits() => HarborInfield.LawnRespectsPits();
}
