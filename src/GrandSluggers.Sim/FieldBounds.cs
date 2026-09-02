namespace GrandSluggers.Sim;

/// <summary>
/// Playable dirt and grass for a park. The wall mesh sits on
/// <see cref="AtBatResolver.FenceAt"/>; gloves plant inside it.
/// Each park's JSON fences — not a Harbor 400.
/// </summary>
public static class FieldBounds
{
    /// <summary>Same pad as <see cref="FlyCatch.WallPlant"/> — warning track, not the crowd.</summary>
    public const double InsideFt = 8;

    /// <summary>Catcher stands here; the backstop is a few feet further.</summary>
    public static double BackstopZ => HomeSet.CatcherZ - 8;

    /// <summary>A little past <see cref="AtBatResolver.FoulLineDeg"/> so a glove can take a ball along the line, not the stands.</summary>
    public const double FoulSprayDeg = AtBatResolver.FoulLineDeg + 5;

    public static double SprayDeg(double x, double z) =>
        Math.Atan2(x, z) * (180.0 / Math.PI);

    public static double DistHome(double x, double z) =>
        Math.Sqrt(x * x + z * z);

    public static bool Inside(Park park, double x, double z)
    {
        var c = Clamp(park, x, z);
        return Diamond.Dist(x, z, c.X, c.Z) < 0.6;
    }

    /// <summary>Clip a glove (or landing) onto the grass for this park.</summary>
    public static (double X, double Z) Clamp(Park park, double x, double z)
    {
        var zz = Math.Max(z, BackstopZ);
        var dist = DistHome(x, zz);
        if (dist < 0.5) return (0, zz);

        var spray = SprayDeg(x, zz);
        if (dist > FieldingResolver.InfieldLipFt && Math.Abs(spray) > FoulSprayDeg)
        {
            spray = Math.Sign(spray) * FoulSprayDeg;
            var rad = spray * (Math.PI / 180.0);
            x = dist * Math.Sin(rad);
            zz = dist * Math.Cos(rad);
            dist = DistHome(x, zz);
        }

        if (zz < 0) return (x, zz);

        var max = Math.Max(12, AtBatResolver.FenceAt(park, spray) - InsideFt);
        if (dist <= max) return (x, zz);
        var u = max / dist;
        return (x * u, zz * u);
    }
}
