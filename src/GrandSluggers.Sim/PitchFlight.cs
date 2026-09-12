namespace GrandSluggers.Sim;

/// <summary>
/// Pitch as a readable object. Fastball flies true, changeup hangs then dumps,
/// curve is two-plane, slider stays true then bites late.
/// u=0 at the rubber, u=1 at the plate. Aim is stick: X in/out, Y up/down, −1..1.
/// </summary>
public static class PitchFlight
{
    // Plate frame geometry: the normalized aim square and where it sits. Presentation, the
    // strike frame, and the umpire all share it, so it stays a constant like Diamond.
    public const double MoundZ = Diamond.Mound;
    public const double PlateY = 2.4;
    public const double PlateScaleX = 1.85;
    public const double PlateScaleY = 1.35;

    public static (double X, double Y) PlateTarget(double aimX, double aimY) =>
        (aimX * PlateScaleX, PlateY + aimY * PlateScaleY);

    /// <summary>Throwing hand, not the torso (pitching.flight.releaseHand*). +X toward first from a RHP.</summary>
    public static (double X, double Y, double Z) Release(double rubberX = 0, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        return (rubberX * f.RubberReleaseX + f.ReleaseHandX, f.ReleaseHandY, MoundZ - f.ReleaseTowardPlate);
    }

    /// <summary>
    /// Super Sluggers air time, not MLB 90 (pitching.flight.arcadeScale, airMin/MaxSec). Charged FB still beats a changeup.
    /// </summary>
    public static double AirSeconds(double mph, RulesTable? rules = null)
    {
        var f = Rules.Or(rules).Pitching.Flight;
        var real = Diamond.Mound / (Math.Max(f.MinMph, mph) * 1.4667);
        return Math.Clamp(real * f.ArcadeScale, f.AirMinSec, f.AirMaxSec);
    }

    public static (double X, double Y, double Z) Point(
        string type, double u, double aimX = 0, double aimY = 0,
        double breakX = 0, bool changeup = false, double rubberX = 0,
        (double X, double Y, double Z)? from = null, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        var f = r.Pitching.Flight;
        var sh = r.Pitching.Shapes;
        u = Math.Clamp(u, 0, 1);
        var (tx, ty) = PlateTarget(aimX + rubberX * f.RubberCrossingX, aimY);
        var rel = from ?? Release(rubberX, r);
        var z = rel.Z * (1 - u);
        var liveChange = changeup || type == "changeup";
        var liveType = liveChange ? "changeup" : type;
        var (x, y, zz) = liveType switch
        {
            "changeup" => Changeup(u, tx, ty, z, rel, sh),
            "curve" => Curve(u, tx, ty, z, rel, sh),
            "slider" => Slider(u, tx, ty, z, rel, sh),
            _ => Fastball(u, tx, ty, z, rel, sh)
        };
        if (breakX != 0)
        {
            var early = Math.Sin(u * Math.PI) * breakX * f.BreakEarly;
            var late = u <= f.BreakLateFrom ? 0 : (u - f.BreakLateFrom) / f.BreakLateSpan;
            x += early + late * late * breakX * f.BreakLate;
        }
        return (x, y, zz);
    }

    /// <summary>The same delivered ball is used by rendering, contact and the umpire.</summary>
    public static (double X, double Y, double Z) Point(PitchCommand pitch, double u,
        string? starPitchId = null, (double X, double Y, double Z)? from = null, RulesTable? rules = null)
    {
        var r = Rules.Or(rules);
        u = Math.Clamp(u, 0, 1);
        var p = Point(pitch.Type, u, pitch.AimX, pitch.AimY, pitch.BreakX,
            pitch.Changeup, pitch.RubberX, from, r);
        if (!pitch.Star) return p;
        var st = r.Pitching.StarShapes;
        return starPitchId switch
        {
            "heatball" => (p.X + Math.Sin(u * st.HeatballWobbleHz) * st.HeatballWobbleFt, p.Y, p.Z),
            "prismball" => (p.X + Math.Sin(u * st.PrismballWobbleHz) * st.PrismballWobbleFt, p.Y, p.Z),
            "charmball" => (p.X + Math.Sin(u * st.CharmballWobbleHz) * st.CharmballWobbleFt, p.Y, p.Z),
            "phonyball" => (p.X + (u > st.PhonyballSwitchAt ? st.PhonyballLateX : st.PhonyballEarlyX), p.Y, p.Z),
            "caskball" => (p.X, p.Y + st.CaskballRise * u, p.Z),
            _ => p
        };
    }

    /// <summary>
    /// Aim a delivery at an intended normalized plate crossing while preserving
    /// its pitch type, curve, rubber position and star movement. Endpoint aim is
    /// affine, so compensating the observed offset gives an exact target.
    /// Call before PreparePitch adds execution error.
    /// </summary>
    public static PitchCommand AimForCrossing(PitchCommand pitch, double targetX, double targetY,
        string? starPitchId = null, RulesTable? rules = null)
    {
        var actual = ContactAim(pitch, starPitchId, rules);
        return pitch with { AimX = pitch.AimX + targetX - actual.X,
            AimY = pitch.AimY + targetY - actual.Y };
    }

    public static (double X, double Y) ContactAim(PitchCommand pitch, string? starPitchId = null, RulesTable? rules = null)
    {
        var p = Point(pitch, 1, starPitchId, rules: rules);
        return (p.X / PlateScaleX, (p.Y - PlateY) / PlateScaleY);
    }

    public static bool InFrontOfLook(double x, double y, double z, CameraShot shot)
    {
        var dx = x - shot.Pos.X;
        var dy = y - shot.Pos.Y;
        var dz = z - shot.Pos.Z;
        var lx = shot.Target.X - shot.Pos.X;
        var ly = shot.Target.Y - shot.Pos.Y;
        var lz = shot.Target.Z - shot.Pos.Z;
        return dx * lx + dy * ly + dz * lz > 0;
    }

    public static double ApparentDeg(double x, double y, double z, CameraShot shot, double diameter)
    {
        var dist = Math.Sqrt(
            (x - shot.Pos.X) * (x - shot.Pos.X) +
            (y - shot.Pos.Y) * (y - shot.Pos.Y) +
            (z - shot.Pos.Z) * (z - shot.Pos.Z));
        return Math.Atan(diameter / Math.Max(0.4, dist)) * (180 / Math.PI);
    }

    static (double X, double Y, double Z) Fastball(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * u - sh.FastballDrop * u * u;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Changeup(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var hang = u < sh.ChangeupHangUntil
            ? u * sh.ChangeupHangRate
            : sh.ChangeupHangUntil * sh.ChangeupHangRate + (u - sh.ChangeupHangUntil) * sh.ChangeupDumpRate;
        hang = Math.Clamp(hang, 0, 1);
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * hang;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Curve(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var sweep = Math.Sin(u * Math.PI) * sh.CurveSweep;
        var hump = Math.Sin(u * Math.PI) * sh.CurveHump;
        var x = rel.X + (tx - rel.X) * u + sweep;
        var y = rel.Y + (ty - rel.Y) * (u * u) + hump;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Slider(double u, double tx, double ty, double z, (double X, double Y, double Z) rel, PitchShapeRules sh)
    {
        var late = u <= sh.SliderBiteFrom ? 0 : (u - sh.SliderBiteFrom) / sh.SliderBiteSpan;
        var bite = late * late * sh.SliderBite;
        var x = rel.X + (tx - rel.X) * u + bite;
        var y = rel.Y + (ty - rel.Y) * u - late * sh.SliderDrop;
        return (x, y, z);
    }
}
