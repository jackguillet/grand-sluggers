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
    public const float InnerHalf = 50f;
    /// <summary>Home→1B / home→3B path width. Thin legs along the foul lines.</summary>
    public const float PathWidth = 10f;
    /// <summary>
    /// Outer arc of the 1B–2B–3B dirt, from the mound. Farther than the
    /// home legs so the back of the diamond is a curve, not a matching frame.
    /// </summary>
    public const float BackR = 92f;
    /// <summary>Round dirt at each bag. Joins the thin paths to the back arc.</summary>
    public const float BagPadR = 12f;
    public const int SkinLoopSegs = 72;
    /// <summary>Center of the dirt slab. Must sit above <see cref="GrassTop"/> so the ring reads at couch.</summary>
    public const float PathY = 0.26f;
    public const float PathThick = 0.24f;
    /// <summary>Dirt top minus grass top. A 0.05-ft lip z-fights and vanishes under the lawn.</summary>
    public const float PathLip = 0.18f;

    /// <summary>Packed dirt circle around each bag. Alias of <see cref="BagPadR"/>.</summary>
    public const float BagDirtR = BagPadR;

    /// <summary>White bag edge. Square, diamond-aligned. Entirely in fair; foul line is the outer edge.</summary>
    public const float BagSize = 1.85f;
    public const float BagY = 0.50f;
    const float InvSqrt2 = 0.70710678f;

    /// <summary>Packed dirt around the plate. Not a 34-ft oval.</summary>
    public const float HomePackedR = 18f;

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
        InnerHalf > 40f && PathWidth < 14f && PathWidth * 4 < BackR;

    public static bool BagIsABag() => BagSize < 2.2f && BagPadR > BagSize && BagPadR < 16f;

    public static bool HomePackedIsAPad() => HomePackedR < 34f;

    public static bool MoundIsAHill() =>
        MoundR > 7f && MoundR < 14f
        && MoundTableR > RubberW * 0.45f && MoundTableR < MoundR * 0.45f
        && MoundH > GrassTop + 0.5f
        && MoundH <= RubberY
        && RubberY - MoundH < 0.08f
        && RubberY > 0.8f && RubberY < 1.4f;

    public static bool StripeReadsAtCouch() => StripeWidth >= 12f && StripeWidth <= 28f;

    /// <summary>1B–2B–3B apron is a mound-centered arc, thicker than the home legs.</summary>
    public static bool BackApronIsCurved()
    {
        var pastSecond = BackR - Dist(0, Diamond.Mound, 0, Diamond.Second.Z);
        return pastSecond > 18 && pastSecond > PathWidth;
    }

    public static bool PathCornersAreRound() => BackApronIsCurved();

    public static float GrassTop => GrassY + GrassThick * 0.5f;
    public static float PathTop => PathY + PathThick * 0.5f;
    public static float PathBottom => PathY - PathThick * 0.5f;

    /// <summary>Chalk sits on the dirt, not in it. Cube is centered at FoulY.</summary>
    public const float FoulWidth = 0.55f;
    public const float FoulThick = 0.12f;
    public const float FoulLip = 0.08f;
    public static float FoulY => PathTop + FoulThick * 0.5f;

    /// <summary>Dirt ring sits on the lawn, not in it. Grass is drawn first; dirt after.</summary>
    public static bool DirtClearsTheLawn() =>
        PathTop >= GrassTop + PathLip && PathBottom >= GrassTop - 0.02f && PathY > GrassY;

    public static bool ChalkClearsTheDirt() =>
        FoulY + FoulThick * 0.5f >= PathTop + FoulLip && FoulY > PathTop;

    /// <summary>
    /// Pillow center. 1B/3B sit fully in fair; <see cref="Diamond.First"/> /
    /// <see cref="Diamond.Third"/> stay the 90-ft foul-line corners.
    /// </summary>
    public static (double X, double Z) BagVisual(int bag)
    {
        var d = BagSize * 0.5;
        return bag switch
        {
            1 => (Diamond.First.X - InvSqrt2 * d, Diamond.First.Z + InvSqrt2 * d),
            3 => (Diamond.Third.X + InvSqrt2 * d, Diamond.Third.Z + InvSqrt2 * d),
            2 => Diamond.Second,
            _ => (0, 0)
        };
    }

    public static bool BagIsInsideTheFoulLine(int bag)
    {
        var p = BagVisual(bag);
        return Math.Abs(p.X) < p.Z - 0.05;
    }

    /// <summary>Chalk strip sits in foul, fair edge on the 90-ft line.</summary>
    public static (float X, float Z) FoulLineCenter(int sign, float midAlong)
    {
        var along = InvSqrt2 * midAlong;
        var o = FoulWidth * 0.5f * InvSqrt2;
        if (sign > 0)
            return (along + o, along - o);
        return (-along - o, along - o);
    }

    public static float CenterZ => (float)(Diamond.Second.Z * 0.5);

    public static float DirtMaxX => BackR;
    public static float DirtMaxZ => (float)Diamond.Mound + BackR;

    /// <summary>L1 diamond of the grass Y. Vertices point at the bags.</summary>
    public static (double X, double Z)[] InnerVerts() =>
    [
        (0, CenterZ - InnerHalf),
        (InnerHalf, CenterZ),
        (0, CenterZ + InnerHalf),
        (-InnerHalf, CenterZ)
    ];

    /// <summary>
    /// Home circle, thin foul-line paths, bag pads, mound, and a curved
    /// 1B–2B–3B apron (inside <see cref="BackR"/> from the mound).
    /// </summary>
    public static bool OnDirt(double x, double z)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return true;
        if (Dist(x, z, 0, Diamond.Mound) <= MoundR) return true;
        if (Dist(x, z, Diamond.First.X, Diamond.First.Z) <= BagPadR) return true;
        if (Dist(x, z, Diamond.Second.X, Diamond.Second.Z) <= BagPadR) return true;
        if (Dist(x, z, Diamond.Third.X, Diamond.Third.Z) <= BagPadR) return true;
        var half = PathWidth * 0.5;
        if (DistToSegment(x, z, 0, 0, Diamond.First.X, Diamond.First.Z) <= half) return true;
        if (DistToSegment(x, z, 0, 0, Diamond.Third.X, Diamond.Third.Z) <= half) return true;
        var l1 = Math.Abs(x) + Math.Abs(z - CenterZ);
        if (l1 <= InnerHalf) return false;
        return Dist(x, z, 0, Diamond.Mound) <= BackR && z >= CenterZ - BagPadR;
    }

    /// <summary>Grass Y inside the diamond, not home dirt and not the mound pad.</summary>
    public static bool OnInfieldGrass(double x, double z)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return false;
        if (Dist(x, z, 0, Diamond.Mound) <= MoundR) return false;
        return Math.Abs(x) + Math.Abs(z - CenterZ) <= InnerHalf;
    }

    /// <summary>
    /// Outer dirt edge around the diamond center. Back is the mound arc;
    /// home legs stay thin until they flare at 1B/3B.
    /// </summary>
    public static (double X, double Z)[] OuterVerts()
    {
        var pts = new (double X, double Z)[SkinLoopSegs];
        for (var i = 0; i < SkinLoopSegs; i++)
        {
            var ang = i * (2 * Math.PI / SkinLoopSegs);
            pts[i] = OuterAt(ang);
        }
        return pts;
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

    static (double X, double Z) OuterAt(double ang)
    {
        var ux = Math.Cos(ang);
        var uz = Math.Sin(ang);
        var inn = InnerOnRay(ux, CenterZ + uz);
        var rInner = Dist(inn.X, inn.Z, 0, CenterZ);
        var rPath = rInner + PathWidth;
        var rHome = RayCircleFar(0, CenterZ, ux, uz, 0, 0, HomePackedR);
        var rBack = RayCircleFar(0, CenterZ, ux, uz, 0, Diamond.Mound, BackR);
        var rBag = Math.Max(
            RayCircleFar(0, CenterZ, ux, uz, Diamond.First.X, Diamond.First.Z, BagPadR),
            Math.Max(
                RayCircleFar(0, CenterZ, ux, uz, Diamond.Second.X, Diamond.Second.Z, BagPadR),
                RayCircleFar(0, CenterZ, ux, uz, Diamond.Third.X, Diamond.Third.Z, BagPadR)));
        var rFront = Math.Max(rPath, Math.Max(rHome, rBag));
        var dHome = AngularDist(ang, -Math.PI / 2);
        var u = (dHome - Math.PI / 4) / (Math.PI / 4);
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        var blend = u * u * (3 - 2 * u);
        var r = rFront + (Math.Max(rBack, rFront) - rFront) * blend;
        r = Math.Max(r, rBag);
        return (ux * r, CenterZ + uz * r);
    }

    static double RayCircleFar(
        double ox, double oz, double dx, double dz,
        double cx, double cz, double r)
    {
        var fx = ox - cx;
        var fz = oz - cz;
        var b = fx * dx + fz * dz;
        var c = fx * fx + fz * fz - r * r;
        var disc = b * b - c;
        if (disc < 0) return 0;
        var s = Math.Sqrt(disc);
        var t = Math.Max(-b - s, -b + s);
        return t > 0.01 ? t : 0;
    }

    static double AngularDist(double a, double b)
    {
        var d = a - b;
        while (d > Math.PI) d -= 2 * Math.PI;
        while (d < -Math.PI) d += 2 * Math.PI;
        return Math.Abs(d);
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
