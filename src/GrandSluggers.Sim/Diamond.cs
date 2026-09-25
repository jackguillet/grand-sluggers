namespace GrandSluggers.Sim;

/// <summary>
/// Feet. Home at origin, +Z toward second/center, +X toward first.
///
/// The process-wide diamond: <see cref="DiamondGeometry"/> of <see cref="Rules.Default"/>. A reader that holds a match's
/// table reads <see cref="DiamondGeometry.Of"/> of that table instead; these members are the readers not yet moved onto it
/// (#1067). <see cref="Dist"/> and <see cref="Order"/> read no table.
/// </summary>
public static class Diamond
{
    static DiamondGeometry Process => DiamondGeometry.Of(Rules.Default);

    public static double Baseline => Process.Baseline;
    public static double Mound => Process.Mound;
    public static (double X, double Z) Home => (0, 0);
    public static (double X, double Z) First => Process.First;
    public static (double X, double Z) Second => Process.Second;
    public static (double X, double Z) Third => Process.Third;
    public static (double X, double Z) Rubber => Process.Rubber;

    /// <summary>Where each fielder stands with nobody on, on the process table (<see cref="DiamondGeometry.Positions"/>).</summary>
    public static IReadOnlyDictionary<string, (double X, double Z)> Positions => Process.Positions;

    public static readonly string[] Order = ["P", "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF"];

    public static double Dist(double x1, double z1, double x2, double z2)
    {
        var dx = x1 - x2;
        var dz = z1 - z2;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public static (double X, double Z) Bag(int bag) => Process.Bag(bag);
}
