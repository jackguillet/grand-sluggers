namespace GrandSluggers.Sim;

/// <summary>
/// Feet. Home at origin, +Z toward second/center, +X toward first.
///
/// What every diamond shares: home is the origin, the nine positions' order, and distance. The bags, the rubber and the
/// starts are a table's (<see cref="DiamondGeometry.Of"/>): a reader takes its match's, never a process-wide one (#1067).
/// </summary>
public static class Diamond
{
    public static (double X, double Z) Home => (0, 0);

    public static readonly string[] Order = ["P", "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF"];

    public static double Dist(double x1, double z1, double x2, double z2)
    {
        var dx = x1 - x2;
        var dz = z1 - z2;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
