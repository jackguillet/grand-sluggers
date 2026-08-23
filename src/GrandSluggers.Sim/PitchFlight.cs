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

    public static (double X, double Y) PlateTarget(double aimX, double aimY) =>
        (aimX * PlateScaleX, PlateY + aimY * PlateScaleY);

    public static (double X, double Y, double Z) Point(string type, double u, double aimX = 0, double aimY = 0)
    {
        u = Math.Clamp(u, 0, 1);
        var (tx, ty) = PlateTarget(aimX, aimY);
        var z = MoundZ * (1 - u);
        return type switch
        {
            "changeup" => Changeup(u, tx, ty, z),
            "curve" => Curve(u, tx, ty, z),
            "slider" => Slider(u, tx, ty, z),
            _ => Fastball(u, tx, ty, z)
        };
    }

    static (double X, double Y, double Z) Fastball(double u, double tx, double ty, double z)
    {
        var x = tx * u;
        var y = ReleaseY + (ty - ReleaseY) * u - 0.35 * u * u;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Changeup(double u, double tx, double ty, double z)
    {
        // Hang with the fastball lane, then dump after ~0.62 of the flight.
        var hang = u < 0.62 ? u * 0.72 : 0.62 * 0.72 + (u - 0.62) * 1.55;
        hang = Math.Clamp(hang, 0, 1);
        return (tx * u, ReleaseY + (ty - ReleaseY) * hang, z);
    }

    static (double X, double Y, double Z) Curve(double u, double tx, double ty, double z)
    {
        var sweep = Math.Sin(u * Math.PI) * 1.7;
        var hump = Math.Sin(u * Math.PI) * 1.35;
        var x = tx * u + sweep;
        var y = ReleaseY + (ty - ReleaseY) * (u * u) + hump;
        return (x, y, z);
    }

    static (double X, double Y, double Z) Slider(double u, double tx, double ty, double z)
    {
        var late = u <= 0.55 ? 0 : (u - 0.55) / 0.45;
        var bite = late * late * 2.4;
        var x = tx * u + bite;
        var y = ReleaseY + (ty - ReleaseY) * u - late * 0.55;
        return (x, y, z);
    }
}
