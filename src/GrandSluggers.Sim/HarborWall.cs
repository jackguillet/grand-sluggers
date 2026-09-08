namespace GrandSluggers.Sim;

/// <summary>
/// Harbor padded wall as a ground loop: round outfield fence, then along
/// foul territory around the dugouts and plate dirt. Not a polar cut
/// through the infield. Fair flies still use <see cref="AtBatResolver.FenceAt"/>.
/// </summary>
public static class HarborWall
{
    public const int OutfieldSegs = 48;
    public const int FoulSegs = 20;
    public const int HomeSegs = 8;
    /// <summary>Outfield inclusive + RF wrap + home + LF wrap minus duplicate poles.</summary>
    public const int WrapSegs = (OutfieldSegs + 1) + FoulSegs + (HomeSegs - 1) + (FoulSegs - 1);
    public const float FoulOffset = 48f;
    public const float HomeZ = -32f;
    public const float DugoutPad = 14f;
    public const bool HasNet = false;
    /// <summary>Authored ring sat on its side in the sky. Boxes follow the loop until the FBX lies in XZ.</summary>
    public const bool DropAuthoredRing = false;

    public static float DugoutClearX => HarborDugout.X + HarborDugout.HalfDeep + DugoutPad;

    static (double X, double Z)[]? _loop;
    static int _loopL, _loopC, _loopR;

    public static (double X, double Z)[] Loop(Park park)
    {
        if (_loop != null
            && _loopL == park.LeftFenceFt
            && _loopC == park.CenterFenceFt
            && _loopR == park.RightFenceFt)
            return _loop;
        _loopL = park.LeftFenceFt;
        _loopC = park.CenterFenceFt;
        _loopR = park.RightFenceFt;
        _loop = BuildLoop(park);
        return _loop;
    }

    public static (double X, double Z) LoopPoint(Park park, int i)
    {
        var loop = Loop(park);
        var n = loop.Length;
        var i0 = ((i % n) + n) % n;
        return loop[i0];
    }

    static (double X, double Z)[] BuildLoop(Park park)
    {
        var pts = new List<(double X, double Z)>(WrapSegs);
        for (var i = 0; i <= OutfieldSegs; i++)
        {
            var spray = -AtBatResolver.FoulLineDeg
                + 2 * AtBatResolver.FoulLineDeg * i / OutfieldSegs;
            pts.Add(FencePoint(park, spray));
        }
        var poleR = AtBatResolver.FenceAt(park, AtBatResolver.FoulLineDeg);
        for (var i = 1; i <= FoulSegs; i++)
            pts.Add(FoulWall(1, poleR * (1 - i / (double)FoulSegs), poleR));
        var rightHome = pts[^1];
        var leftHome = FoulWall(-1, 0, poleR);
        for (var i = 1; i < HomeSegs; i++)
        {
            var t = i / (double)HomeSegs;
            pts.Add(LerpPt(rightHome, (0, HomeZ), leftHome, t));
        }
        var poleL = AtBatResolver.FenceAt(park, -AtBatResolver.FoulLineDeg);
        for (var i = 1; i < FoulSegs; i++)
            pts.Add(FoulWall(-1, poleL * (i / (double)FoulSegs), poleL));
        return pts.ToArray();
    }

    /// <summary>
    /// Along the foul line toward home, offset into foul so the wall stays
    /// off the dirt and outside the dugout. Offset is 0 at the pole.
    /// </summary>
    static (double X, double Z) FoulWall(int sign, double alongFt, double poleFt)
    {
        var inv = 0.7071067811865476;
        var s = Math.Max(0, alongFt);
        var u = poleFt < 1 ? 1 : 1 - s / poleFt;
        u = Math.Clamp(u, 0, 1);
        var off = FoulOffset * (u * u * (3 - 2 * u));
        return (sign * (inv * s + inv * off), inv * s - inv * off);
    }

    static (double X, double Z) FencePoint(Park park, double sprayDeg)
    {
        var r = AtBatResolver.FenceAt(park, sprayDeg);
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    static (double X, double Z) LerpPt(
        (double X, double Z) a, (double X, double Z) b, (double X, double Z) c, double t)
    {
        // Quadratic through 1B-home, behind home, 3B-home.
        var omt = 1 - t;
        return (
            omt * omt * a.X + 2 * omt * t * b.X + t * t * c.X,
            omt * omt * a.Z + 2 * omt * t * b.Z + t * t * c.Z);
    }

    public static (double X, double Z) TrackInner(Park park, int i)
    {
        var p = LoopPoint(park, i);
        var o = Outward(park, i);
        var w = ParkDiamond.TrackWidth;
        return (p.X - o.X * w, p.Z - o.Z * w);
    }

    public static (double X, double Z) TrackOuter(Park park, int i)
    {
        var p = LoopPoint(park, i);
        var o = Outward(park, i);
        var w = ParkDiamond.TrackWallInset;
        return (p.X - o.X * w, p.Z - o.Z * w);
    }

    public static (double X, double Z) Outward(Park park, int i)
    {
        var a = LoopPoint(park, i);
        var b = LoopPoint(park, i + 1);
        var tx = b.X - a.X;
        var tz = b.Z - a.Z;
        var len = Math.Sqrt(tx * tx + tz * tz);
        if (len < 1e-6) return (0, 1);
        tx /= len;
        tz /= len;
        // Cross(up, tangent): (-tz, tx) in XZ.
        var ox = -tz;
        var oz = tx;
        var mx = (a.X + b.X) * 0.5;
        var mz = (a.Z + b.Z) * 0.5;
        if (ox * mx + oz * mz < 0)
        {
            ox = -ox;
            oz = -oz;
        }
        return (ox, oz);
    }

    public static bool WrapStaysInFoul(Park park)
    {
        foreach (var p in Loop(park))
        {
            if (p.Z < 8) continue;
            var spray = Math.Atan2(p.X, p.Z) * (180.0 / Math.PI);
            if (Math.Abs(spray) <= AtBatResolver.FoulLineDeg + 1) continue;
            if (Math.Abs(p.X) < p.Z - 2) return false;
            if (ParkDiamond.OnDirt(p.X, p.Z)) return false;
        }
        return true;
    }

    public static bool WrapsTheDiamond(Park park)
    {
        var loop = Loop(park);
        if (loop.Length < 40) return false;
        var home = loop.OrderBy(p => p.Z).First();
        if (home.Z > HomeSet.CatcherZ - 8) return false;
        if (home.Z < HarborStands.HomeZ0 - 8) return false;
        var dug = (HarborDugout.X, HarborDugout.Z);
        var minDug = loop.Min(p => Diamond.Dist(p.X, p.Z, dug.X, dug.Z));
        if (minDug < HarborDugout.HalfDeep + 6) return false;
        var cf = FencePoint(park, 0);
        if (loop.Min(p => Diamond.Dist(p.X, p.Z, cf.X, cf.Z)) > 4) return false;
        return WrapStaysInFoul(park) && !HasNet && !DropAuthoredRing;
    }
}
