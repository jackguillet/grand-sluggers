using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The shipped table's diamond, for the tests that measure against it (#1067). The sim's own <see cref="GrandSluggers.Sim.Diamond"/>
/// reads no table: a reader in the sim takes its match's <see cref="DiamondGeometry"/>. Inside this namespace this class wins the
/// name, so a test reads the shipped bags as it always did.
/// </summary>
static class Diamond
{
    static DiamondGeometry Shipped => DiamondGeometry.Of(Rules.Default);

    public static double Baseline => Shipped.Baseline;
    public static double Mound => Shipped.Mound;
    public static (double X, double Z) Home => GrandSluggers.Sim.Diamond.Home;
    public static (double X, double Z) First => Shipped.First;
    public static (double X, double Z) Second => Shipped.Second;
    public static (double X, double Z) Third => Shipped.Third;
    public static (double X, double Z) Rubber => Shipped.Rubber;
    public static IReadOnlyDictionary<string, (double X, double Z)> Positions => Shipped.Positions;
    public static string[] Order => GrandSluggers.Sim.Diamond.Order;
    public static (double X, double Z) Bag(int bag) => Shipped.Bag(bag);
    public static double Dist(double x1, double z1, double x2, double z2) => GrandSluggers.Sim.Diamond.Dist(x1, z1, x2, z2);
}
