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
    /// <summary>One side (CF→RF→home) mirrored. Must stay even.</summary>
    public const int WrapSegs = 2 * (OutfieldSegs / 2 + 1 + FoulSegs + HomeSegs / 2) - 2;
    /// <summary>
    /// Hip wall offset from the foul line along the infield. Sits just behind
    /// the dugout. Flares to the pole. Home backstop stays at <see cref="HomeZ"/>.
    /// </summary>
    public const float FoulOffset = 36f;
    /// <summary>Backstop distance behind the plate. MLB prefers ~60 ft.</summary>
    public const float HomeZ = -56f;
    public const float DugoutPad = 18f;
    public const float OutfieldHeight = 26f;
    /// <summary>Hip-high rail around the infield, dugouts, and home.</summary>
    public const float HipHeight = 4.2f;
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
        // Build CF → RF pole → behind home, then mirror so 1B/3B match.
        var half = new List<(double X, double Z)>(WrapSegs / 2 + 2);
        for (var i = OutfieldSegs / 2; i <= OutfieldSegs; i++)
        {
            var spray = -AtBatResolver.FoulLineDeg
                + 2 * AtBatResolver.FoulLineDeg * i / OutfieldSegs;
            half.Add(FencePoint(park, spray));
        }
        var poleR = AtBatResolver.FenceAt(park, AtBatResolver.FoulLineDeg);
        for (var i = 1; i <= FoulSegs; i++)
            half.Add(FoulWall(1, poleR * (1 - i / (double)FoulSegs), poleR));
        var rightHome = half[^1];
        var home = (0.0, (double)HomeZ);
        for (var i = 1; i <= HomeSegs / 2; i++)
        {
            var t = i / (double)(HomeSegs / 2);
            half.Add(Lerp2(rightHome, home, t));
        }
        var pts = new List<(double X, double Z)>(half);
        for (var i = half.Count - 2; i >= 1; i--)
            pts.Add((-half[i].X, half[i].Z));
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
        // Parallel to the line through the infield, then flare to the pole.
        var flareStart = 95;
        var u = s <= flareStart || poleFt <= flareStart
            ? 1
            : 1 - Math.Clamp((s - flareStart) / (poleFt - flareStart), 0, 1);
        u = u * u * (3 - 2 * u);
        var off = FoulOffset * u;
        return (sign * (inv * s + inv * off), inv * s - inv * off);
    }

    static (double X, double Z) FencePoint(Park park, double sprayDeg)
    {
        var r = AtBatResolver.FenceAt(park, sprayDeg);
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    static (double X, double Z) Lerp2((double X, double Z) a, (double X, double Z) b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        t = t * t * (3 - 2 * t);
        return (a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t);
    }

    public static bool LoopIsSymmetric(Park park)
    {
        var loop = Loop(park);
        if (loop.Length != WrapSegs) return false;
        foreach (var p in loop)
        {
            var ok = false;
            foreach (var q in loop)
            {
                if (Math.Abs(q.X + p.X) < 1.2 && Math.Abs(q.Z - p.Z) < 1.2)
                {
                    ok = true;
                    break;
                }
            }
            if (!ok) return false;
        }
        return true;
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

    /// <summary>
    /// Tall in the outfield, tapers to hip height along the foul wrap so the
    /// side wall is a rail, not a 26-ft fence through the dugouts.
    /// </summary>
    public static float Height(Park park, int i)
    {
        var p = LoopPoint(park, i);
        var spray = Math.Atan2(p.X, p.Z) * (180.0 / Math.PI);
        if (Math.Abs(spray) <= AtBatResolver.FoulLineDeg + 0.5)
            return OutfieldHeight;
        const double hipZ = 95;
        if (p.Z <= hipZ) return HipHeight;
        var poleZ = Math.Cos(AtBatResolver.FoulLineDeg * Math.PI / 180.0)
            * AtBatResolver.FenceAt(park, Math.Sign(p.X) * AtBatResolver.FoulLineDeg);
        var u = (p.Z - hipZ) / Math.Max(20, poleZ - hipZ);
        u = Math.Clamp(u, 0, 1);
        var s = u * u * (3 - 2 * u);
        return HipHeight + (OutfieldHeight - HipHeight) * (float)s;
    }

    public static bool OutfieldIsTallerThanTheHip() =>
        OutfieldHeight >= 18f && HipHeight >= 3.2f && HipHeight <= 5.5f
        && OutfieldHeight > HipHeight * 3f;

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
        // Center sits HalfDeep behind the rail. The hip wall IS that rail.
        if (Math.Abs(minDug - HarborDugout.HalfDeep) > 4) return false;
        var cf = FencePoint(park, 0);
        if (loop.Min(p => Diamond.Dist(p.X, p.Z, cf.X, cf.Z)) > 4) return false;
        return WrapStaysInFoul(park) && LoopIsSymmetric(park)
            && !HasNet && !DropAuthoredRing
            && OutfieldIsTallerThanTheHip()
            && Height(park, 0) >= OutfieldHeight - 0.1f;
    }
}
