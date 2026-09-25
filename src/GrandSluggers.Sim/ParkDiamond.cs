namespace GrandSluggers.Sim;

/// <summary>
/// Shared diamond ground: dirt paths, mound, striped grass, foul poles, warning track.
/// Harbor fills these now. Other parks reuse the same outlines when they get a kit —
/// fence distances come from <see cref="Park"/>, bags and the rubber from the table's <see cref="DiamondGeometry"/>.
/// </summary>
public static class ParkDiamond
{
    /// <summary>
    /// Grass Y: L1 diamond around second/mound. Bags sit outside this, on the dirt.
    /// <c>data/rules/infield.json</c> carries it (#729), because it is measured from the bags and
    /// has to travel with them.
    /// </summary>
    public static float InnerHalf(DiamondGeometry d) => (float)d.InnerHalfFt;
    /// <summary>Home→1B / home→3B path width. Thin legs along the foul lines.</summary>
    public const float PathWidth = 10f;
    /// <summary>
    /// Outer arc of the 1B–2B–3B dirt, from the mound. Farther than the
    /// home legs so the back of the diamond is a curve, not a matching frame.
    /// <c>data/rules/infield.json</c> carries it (#729), for the same reason as
    /// <see cref="InnerHalf"/>: it is measured from the mound, which moved.
    /// </summary>
    public static float BackR(DiamondGeometry d) => (float)d.BackArcFt;
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

    /// <summary>
    /// White bag. Official is 15″; Harbor is a cartoon pillow that still
    /// reads from the overhead. Entirely in fair; foul line is the outer edge.
    /// </summary>
    public const float BagSize = 4f;
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
    /// <summary>Ring samples. Boxes sawtooth; the dress is an annulus along <see cref="TrackInner"/>.</summary>
    public const int TrackSegs = 64;
    public const float TrackY = 0.14f;
    public const float TrackThick = 0.22f;
    /// <summary>Outer ring tucks under the wall face, not past it.</summary>
    public const float TrackWallInset = 0.5f;

    /// <summary>Yellow pole well above the park's wall. Photo-scale: several times the fence.</summary>
    public const float PoleHeight = 72f;
    public const float PoleRadius = 0.62f;
    /// <summary>Grate from the wall cap up the shaft. Faces fair, sits on the fair side of the pole.</summary>
    public const float PoleScreenH = 38f;
    public const float PoleScreenW = 5.6f;
    public const float PoleScreenY = 45f;
    public const float PoleScreenThick = 0.22f;

    /// <summary>From the pole into fair, perpendicular to the foul line.</summary>
    public static (double X, double Z) FairInward(int sign)
    {
        const double inv = 0.7071067811865476;
        var s = Math.Sign(sign);
        return (-s * inv, inv);
    }

    public static bool ScreenFacesFair(Park park)
    {
        var rf = FairInward(1);
        var lf = FairInward(-1);
        return rf.X < 0 && rf.Z > 0 && lf.X > 0 && lf.Z > 0
            && PoleHeight > HarborWall.OutfieldHeight(park) * 2.2f
            && PoleScreenH > HarborWall.OutfieldHeight(park);
    }

    /// <summary>Mow stripe width. Bands of constant X — home → CF, vertical in the overhead.</summary>
    public const float StripeWidth = 18f;
    public const float GrassY = 0.08f;
    public const float GrassThick = 0.12f;
    public const float GrassZ0 = -32f;
    /// <summary>Foul-territory grass past the 45° line.</summary>
    public const float FoulGrassFt = 36f;

    public static bool PathIsNotALake(DiamondGeometry d) =>
        InnerHalf(d) > 40f && PathWidth < 14f && PathWidth * 4 < BackR(d);

    public static bool BagIsABag() =>
        BagSize >= 3.2f && BagSize <= 5f && BagPadR > BagSize * 2f && BagPadR < 16f;

    public static bool HomePackedIsAPad() => HomePackedR < 34f;

    public static bool MoundIsAHill() =>
        MoundR > 7f && MoundR < 14f
        && MoundTableR > RubberW * 0.45f && MoundTableR < MoundR * 0.45f
        && MoundH > GrassTop + 0.5f
        && MoundH <= RubberY
        && RubberY - MoundH < 0.08f
        && RubberY > 0.8f && RubberY < 1.4f;

    /// <summary>
    /// Feet Y on the hill. Rubber table is <see cref="RubberY"/> (MLB 10″ cartoon-tall).
    /// Y=0 buries the pitcher in the mound.
    /// </summary>
    public static float StandY(double x, double z, DiamondGeometry d)
    {
        var r = Diamond.Dist(x, z, 0, d.Mound);
        if (r <= MoundTableR) return RubberY;
        if (r >= MoundR) return 0f;
        var u = (MoundR - r) / (MoundR - MoundTableR);
        return (float)(RubberY * u);
    }

    public static bool PitcherStandsOnTheHill(DiamondGeometry d) =>
        StandY(0, d.Mound, d) >= RubberY - 0.05f
        && StandY(0, 0, d) < 0.2f
        && StandY(MoundR + 1, d.Mound, d) < 0.05f;

    public static bool StripeReadsAtCouch() => StripeWidth >= 12f && StripeWidth <= 28f;

    /// <summary>1B–2B–3B apron is a mound-centered arc, thicker than the home legs.</summary>
    public static bool BackApronIsCurved(DiamondGeometry d)
    {
        var pastSecond = BackR(d) - Dist(0, d.Mound, 0, d.Second.Z);
        return pastSecond > 18 && pastSecond > PathWidth;
    }

    public static bool PathCornersAreRound(DiamondGeometry d) => BackApronIsCurved(d);

    public static float GrassTop => GrassY + GrassThick * 0.5f;
    public static float PathTop => PathY + PathThick * 0.5f;
    public static float PathBottom => PathY - PathThick * 0.5f;

    /// <summary>Chalk sits on the dirt, not in it. Cube is centered at FoulY. Width matches home chalk (4″).</summary>
    public const float FoulWidth = 4f / 12f;
    public const float FoulThick = 0.12f;
    public const float FoulLip = 0.08f;
    public static float FoulY => PathTop + FoulThick * 0.5f;

    /// <summary>Dirt ring sits on the lawn, not in it. Grass is drawn first; dirt after.</summary>
    public static bool DirtClearsTheLawn() =>
        PathTop >= GrassTop + PathLip && PathBottom >= GrassTop - 0.02f && PathY > GrassY;

    public static bool ChalkClearsTheDirt() =>
        FoulY + FoulThick * 0.5f >= PathTop + FoulLip && FoulY > PathTop;

    /// <summary>
    /// Pillow center. 1B/3B sit fully in fair; the table's <see cref="DiamondGeometry.First"/> /
    /// <see cref="DiamondGeometry.Third"/> stay the foul-line corners.
    /// </summary>
    public static (double X, double Z) BagVisual(int bag, DiamondGeometry d)
    {
        var half = BagSize * 0.5;
        return bag switch
        {
            1 => (d.First.X - InvSqrt2 * half, d.First.Z + InvSqrt2 * half),
            3 => (d.Third.X + InvSqrt2 * half, d.Third.Z + InvSqrt2 * half),
            2 => d.Second,
            _ => (0, 0)
        };
    }

    public static bool BagIsInsideTheFoulLine(int bag, DiamondGeometry d)
    {
        var p = BagVisual(bag, d);
        return Math.Abs(p.X) < p.Z - 0.05;
    }

    /// <summary>1B and 3B lines from home are perpendicular and equal. Always 90°.</summary>
    public static bool FoulLinesAreSquare(DiamondGeometry d)
    {
        var f = d.First;
        var t = d.Third;
        var dot = f.X * t.X + f.Z * t.Z;
        var magF = Dist(0, 0, f.X, f.Z);
        var magT = Dist(0, 0, t.X, t.Z);
        var yaw = Math.Abs(AtBatResolver.FoulLineDeg);
        return Math.Abs(dot) < 0.5
            && Math.Abs(magF - d.Baseline) < 0.2
            && Math.Abs(magT - d.Baseline) < 0.2
            && Math.Abs(yaw - 45) < 0.01;
    }

    public static float FoulYaw(int sign) =>
        (float)(Math.Sign(sign) * AtBatResolver.FoulLineDeg);

    /// <summary>Chalk strip sits in foul, fair edge on the 90-ft line.</summary>
    public static (float X, float Z) FoulLineCenter(int sign, float midAlong)
    {
        var along = InvSqrt2 * midAlong;
        var o = FoulWidth * 0.5f * InvSqrt2;
        if (sign > 0)
            return (along + o, along - o);
        return (-along - o, along - o);
    }

    public static float CenterZ(DiamondGeometry d) => (float)(d.Second.Z * 0.5);

    public static float DirtMaxX(DiamondGeometry d) => BackR(d);
    public static float DirtMaxZ(DiamondGeometry d) => (float)d.Mound + BackR(d);

    /// <summary>L1 diamond of the grass Y. Vertices point at the bags.</summary>
    public static (double X, double Z)[] InnerVerts(DiamondGeometry d) =>
    [
        (0, CenterZ(d) - InnerHalf(d)),
        (InnerHalf(d), CenterZ(d)),
        (0, CenterZ(d) + InnerHalf(d)),
        (-InnerHalf(d), CenterZ(d))
    ];

    /// <summary>
    /// Home circle, thin foul-line paths, bag pads, mound, and a curved
    /// 1B–2B–3B apron (inside <see cref="BackR"/> from the mound).
    /// </summary>
    public static bool OnDirt(double x, double z, DiamondGeometry d)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return true;
        if (Dist(x, z, 0, d.Mound) <= MoundR) return true;
        if (Dist(x, z, d.First.X, d.First.Z) <= BagPadR) return true;
        if (Dist(x, z, d.Second.X, d.Second.Z) <= BagPadR) return true;
        if (Dist(x, z, d.Third.X, d.Third.Z) <= BagPadR) return true;
        var half = PathWidth * 0.5;
        if (DistToSegment(x, z, 0, 0, d.First.X, d.First.Z) <= half) return true;
        if (DistToSegment(x, z, 0, 0, d.Third.X, d.Third.Z) <= half) return true;
        var l1 = Math.Abs(x) + Math.Abs(z - CenterZ(d));
        if (l1 <= InnerHalf(d)) return false;
        return Dist(x, z, 0, d.Mound) <= BackR(d) && z >= CenterZ(d) - BagPadR;
    }

    /// <summary>Grass Y inside the diamond, not home dirt and not the mound pad.</summary>
    public static bool OnInfieldGrass(double x, double z, DiamondGeometry d)
    {
        if (Dist(x, z, 0, 0) <= HomePackedR) return false;
        if (Dist(x, z, 0, d.Mound) <= MoundR) return false;
        return Math.Abs(x) + Math.Abs(z - CenterZ(d)) <= InnerHalf(d);
    }

    /// <summary>
    /// Outer dirt edge around the diamond center. Back is the mound arc;
    /// home legs stay thin until they flare at 1B/3B.
    /// </summary>
    public static (double X, double Z)[] OuterVerts(DiamondGeometry d)
    {
        var pts = new (double X, double Z)[SkinLoopSegs];
        for (var i = 0; i < SkinLoopSegs; i++)
        {
            var ang = i * (2 * Math.PI / SkinLoopSegs);
            pts[i] = OuterAt(ang, d);
        }
        return pts;
    }

    public static (double X, double Z) InnerOnRay(double x, double z, DiamondGeometry d)
    {
        var dx = x;
        var dz = z - CenterZ(d);
        var l1 = Math.Abs(dx) + Math.Abs(dz);
        if (l1 < 1e-6) return (0, CenterZ(d));
        var s = InnerHalf(d) / l1;
        return (dx * s, CenterZ(d) + dz * s);
    }

    static (double X, double Z) OuterAt(double ang, DiamondGeometry d)
    {
        var ux = Math.Cos(ang);
        var uz = Math.Sin(ang);
        var inn = InnerOnRay(ux, CenterZ(d) + uz, d);
        var rInner = Dist(inn.X, inn.Z, 0, CenterZ(d));
        var rPath = rInner + PathWidth;
        var rHome = RayCircleFar(0, CenterZ(d), ux, uz, 0, 0, HomePackedR);
        var rBack = RayCircleFar(0, CenterZ(d), ux, uz, 0, d.Mound, BackR(d));
        var rBag = Math.Max(
            RayCircleFar(0, CenterZ(d), ux, uz, d.First.X, d.First.Z, BagPadR),
            Math.Max(
                RayCircleFar(0, CenterZ(d), ux, uz, d.Second.X, d.Second.Z, BagPadR),
                RayCircleFar(0, CenterZ(d), ux, uz, d.Third.X, d.Third.Z, BagPadR)));
        var rFront = Math.Max(rPath, Math.Max(rHome, rBag));
        var dHome = AngularDist(ang, -Math.PI / 2);
        var u = (dHome - Math.PI / 4) / (Math.PI / 4);
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        var blend = u * u * (3 - 2 * u);
        var r = rFront + (Math.Max(rBack, rFront) - rFront) * blend;
        r = Math.Max(r, rBag);
        return (ux * r, CenterZ(d) + uz * r);
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

    /// <summary>Band 0 sits on the home→CF axis so the mow is symmetric with the diamond.</summary>
    public static float StripeCenterX(int i) => i * StripeWidth;

    public static bool StripesAreCenteredOnTheField() =>
        StripeCenterX(0) == 0f && StripeCenterX(1) == -StripeCenterX(-1) && StripeCenterX(1) == StripeWidth;

    public static (double X, double Z) FoulPole(Park park, int sign)
    {
        var spray = Math.Sign(sign) * AtBatResolver.FoulLineDeg;
        var fence = AtBatResolver.FenceAt(park, spray);
        var rad = spray * Math.PI / 180.0;
        return (Math.Sin(rad) * fence, Math.Cos(rad) * fence);
    }

    public static double TrackMid(Park park, double sprayDeg) =>
        AtBatResolver.FenceAt(park, sprayDeg) - TrackWidth * 0.5;

    public static (double X, double Z) TrackInner(Park park, double sprayDeg) =>
        TrackAt(sprayDeg, AtBatResolver.FenceAt(park, sprayDeg) - TrackWidth);

    public static (double X, double Z) TrackOuter(Park park, double sprayDeg) =>
        TrackAt(sprayDeg, AtBatResolver.FenceAt(park, sprayDeg) - TrackWallInset);

    static (double X, double Z) TrackAt(double sprayDeg, double r)
    {
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    public static double TrackSpray(int i)
    {
        var n = TrackSegs;
        var i0 = Math.Clamp(i, 0, n);
        return -AtBatResolver.FoulLineDeg + 2 * AtBatResolver.FoulLineDeg * i0 / n;
    }

    /// <summary>
    /// Inner edge is the fence offset, not the corners of equal-width slabs.
    /// </summary>
    public static bool TrackFollowsTheFenceArc(Park park)
    {
        for (var i = 0; i <= TrackSegs; i++)
        {
            var spray = TrackSpray(i);
            var inner = TrackInner(park, spray);
            var r = Dist(0, 0, inner.X, inner.Z);
            var expect = AtBatResolver.FenceAt(park, spray) - TrackWidth;
            if (Math.Abs(r - expect) > 0.5) return false;
        }
        var cf = TrackInner(park, 0);
        return Math.Abs(Dist(0, 0, cf.X, cf.Z) - (park.CenterFenceFt - TrackWidth)) < 0.6;
    }

    public static float GrassHalfWidth(float z) =>
        Math.Max(40f, Math.Abs(z) + FoulGrassFt);

    public static float GrassZ1(Park park) =>
        (float)park.CenterFenceFt - TrackWidth;

    /// <summary>
    /// Lawn slab covers this XZ. Dirt, track, and dugout pits sit on or
    /// punch through it — the mow does not stop at <see cref="DirtMaxZ"/>.
    /// </summary>
    public static bool LawnCovers(double x, double z, Park park)
    {
        if (z < GrassZ0 || z > GrassZ1(park)) return false;
        if (Math.Abs(x) > GrassHalfWidth((float)Math.Max(z, 0))) return false;
        if (HarborDugout.InPitHole(x, z)) return false;
        return true;
    }

    public static bool TrackIsInsideTheWall(Park park, RulesTable rules) =>
        TrackWidth > 8f && TrackWidth < 24f
        && TrackMid(park, 0) > rules.Flight.Classes.InfieldLipFt
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

    public static bool LawnRespectsPits(DiamondGeometry d) => HarborInfield.LawnRespectsPits(d);
}
