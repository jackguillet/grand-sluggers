namespace GrandSluggers.Sim;

/// <summary>
/// Harbor home-run wall as a full loop: outfield fence, then in around the
/// dugouts and plate dirt. Replaces the chain-link net behind home.
/// Fair flies still use <see cref="AtBatResolver.FenceAt"/>.
/// </summary>
public static class HarborWall
{
    public const int WrapSegs = 120;
    public const float HomeRadius = 34f;
    public const float DugoutPad = 14f;
    public const bool HasNet = false;

    public static float DugoutClearX => HarborDugout.X + HarborDugout.HalfDeep + DugoutPad;
    public static float DugoutClearZ => HarborDugout.Z + HarborDugout.HalfAlong + 10f;
    public static double DugoutR =>
        Math.Sqrt(DugoutClearX * DugoutClearX + DugoutClearZ * DugoutClearZ);
    public static double DugoutSprayDeg =>
        Math.Atan2(DugoutClearX, DugoutClearZ) * (180.0 / Math.PI);

    public static double WrapSpray(int i)
    {
        var n = WrapSegs;
        var i0 = ((i % n) + n) % n;
        return -180.0 + 360.0 * i0 / n;
    }

    public static double Radius(Park park, double sprayDeg)
    {
        var s = Norm(sprayDeg);
        var a = Math.Abs(s);
        if (a <= AtBatResolver.FoulLineDeg)
            return AtBatResolver.FenceAt(park, s);
        var pole = AtBatResolver.FenceAt(park, Math.Sign(s) * AtBatResolver.FoulLineDeg);
        var side = DugoutClearX;
        var knots = new (double Deg, double R)[]
        {
            (AtBatResolver.FoulLineDeg, pole),
            (DugoutSprayDeg, DugoutR),
            (95, side),
            (180, HomeRadius)
        };
        return LerpKnots(a, knots);
    }

    public static (double X, double Z) Point(Park park, double sprayDeg)
    {
        var r = Radius(park, sprayDeg);
        var rad = Norm(sprayDeg) * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    public static (double X, double Z) TrackInner(Park park, double sprayDeg)
    {
        var r = Math.Max(10, Radius(park, sprayDeg) - ParkDiamond.TrackWidth);
        var rad = Norm(sprayDeg) * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    public static (double X, double Z) TrackOuter(Park park, double sprayDeg)
    {
        var r = Math.Max(12, Radius(park, sprayDeg) - ParkDiamond.TrackWallInset);
        var rad = Norm(sprayDeg) * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    public static bool WrapsTheDiamond(Park park)
    {
        var home = Point(park, 180);
        if (home.Z > HomeSet.CatcherZ - 8) return false;
        if (home.Z < HarborStands.HomeZ0 - 6) return false;
        var dug = Diamond.Dist(HarborDugout.X, HarborDugout.Z, 0, 0);
        if (Radius(park, DugoutSprayDeg) < dug + HarborDugout.HalfDeep + 6) return false;
        if (Math.Abs(Radius(park, 0) - park.CenterFenceFt) > 2) return false;
        if (Math.Abs(Radius(park, 45) - park.RightFenceFt) > 2) return false;
        return WrapSegs >= 96;
    }

    static double Norm(double sprayDeg)
    {
        var s = sprayDeg % 360;
        if (s > 180) s -= 360;
        if (s < -180) s += 360;
        return s;
    }

    static double LerpKnots(double a, (double Deg, double R)[] knots)
    {
        if (a <= knots[0].Deg) return knots[0].R;
        for (var i = 0; i < knots.Length - 1; i++)
        {
            if (a > knots[i + 1].Deg) continue;
            var span = knots[i + 1].Deg - knots[i].Deg;
            var u = span < 1e-6 ? 1 : (a - knots[i].Deg) / span;
            u = Math.Clamp(u, 0, 1);
            var s = u * u * (3 - 2 * u);
            return knots[i].R + (knots[i + 1].R - knots[i].R) * s;
        }
        return knots[^1].R;
    }
}
