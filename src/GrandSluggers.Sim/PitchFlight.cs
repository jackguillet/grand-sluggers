namespace GrandSluggers.Sim;

/// <summary>
/// Pitch as a readable object. Fastball flies true, changeup hangs then dumps,
/// curve is two-plane, slider stays true then bites late.
/// u=0 at the rubber, u=1 at the plate. Aim is stick: X in/out, Y up/down, −1..1.
/// </summary>
public static class PitchFlight
{
    public const double MoundZ = 60.5;
    public const double ReleaseY = 5.4;
    public const double PlateY = 2.4;
    public const double PlateScaleX = 1.85;
    public const double PlateScaleY = 1.35;
    /// <summary>Throwing hand, not the torso. +X toward first from a RHP.</summary>
    public const double ReleaseHandX = 1.55;
    public const double ReleaseHandY = 6.2;
    public const double ReleaseTowardPlate = 2.6;

    public static (double X, double Y) PlateTarget(double aimX, double aimY) =>
        (aimX * PlateScaleX, PlateY + aimY * PlateScaleY);

    public static (double X, double Y, double Z) Release(double rubberX = 0) =>
        (rubberX * 2.2 + ReleaseHandX, ReleaseHandY, MoundZ - ReleaseTowardPlate);

    public static (double X, double Y, double Z) Point(
        string type, double u, double aimX = 0, double aimY = 0,
        double breakX = 0, bool changeup = false, double rubberX = 0)
    {
        u = Math.Clamp(u, 0, 1);
        var (tx, ty) = PlateTarget(aimX + rubberX * 0.35, aimY);
        var rel = Release(rubberX);
        var z = rel.Z * (1 - u);
        var liveChange = changeup || type == "changeup";
        var liveType = liveChange ? "changeup" : type;
        var (x, y, zz) = liveType switch
        {
            "changeup" => Changeup(u, tx, ty, z, rel),
            "curve" => Curve(u, tx, ty, z, rel),
            "slider" => Slider(u, tx, ty, z, rel),
            _ => Fastball(u, tx, ty, z, rel)
        };
        if (breakX != 0)
        {
            var early = Math.Sin(u * Math.PI) * breakX * 1.55;
            var late = u <= 0.55 ? 0 : (u - 0.55) / 0.45;
            x += early + late * late * breakX * 1.8;
        }
        return (x, y, zz);
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

    static (double X, double Y, double Z) Fastball(double u, double tx, double ty, double z, (double X, double Y, double Z) rel)
    {
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * u - 0.35 * u * u;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Changeup(double u, double tx, double ty, double z, (double X, double Y, double Z) rel)
    {
        var hang = u < 0.62 ? u * 0.72 : 0.62 * 0.72 + (u - 0.62) * 1.55;
        hang = Math.Clamp(hang, 0, 1);
        var x = rel.X + (tx - rel.X) * u;
        var y = rel.Y + (ty - rel.Y) * hang;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Curve(double u, double tx, double ty, double z, (double X, double Y, double Z) rel)
    {
        var sweep = Math.Sin(u * Math.PI) * 1.7;
        var hump = Math.Sin(u * Math.PI) * 1.35;
        var x = rel.X + (tx - rel.X) * u + sweep;
        var y = rel.Y + (ty - rel.Y) * (u * u) + hump;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Slider(double u, double tx, double ty, double z, (double X, double Y, double Z) rel)
    {
        var late = u <= 0.55 ? 0 : (u - 0.55) / 0.45;
        var bite = late * late * 2.4;
        var x = rel.X + (tx - rel.X) * u + bite;
        var y = rel.Y + (ty - rel.Y) * u - late * 0.55;
        return (x, y, z);
    }
}
