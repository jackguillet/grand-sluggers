namespace GrandSluggers.Sim;

/// <summary>
/// Feet. Home at origin, +Z toward second/center, +X toward first.
///
/// The numbers live in <c>data/rules/infield.json</c> (<see cref="InfieldRules"/>) and are read
/// through <see cref="Rules.Default"/> — the one process-wide table. Every park shares this
/// diamond, so a single global set is the honest model; nothing may change it mid-run, because
/// tests execute in parallel and a mutable global would make them interfere. To play a different
/// infield, point the process at a different data root (<c>GRAND_SLUGGERS_DATA</c>) and compare
/// the two runs.
/// </summary>
public static class Diamond
{
    static InfieldRules In => Rules.Default.Infield;

    public static double Baseline => In.BaselineFt;
    public static double Mound => In.MoundFt;
    public static (double X, double Z) Home => (0, 0);
    public static (double X, double Z) First => (In.CornerFt, In.CornerFt);
    public static (double X, double Z) Second => (0, In.SecondFt);
    public static (double X, double Z) Third => (-In.CornerFt, In.CornerFt);
    public static (double X, double Z) Rubber => (0, In.MoundFt);

    static IReadOnlyDictionary<string, (double X, double Z)>? _positions;

    /// <summary>
    /// Where each fielder stands with nobody on. Built once — the geometry behind it never moves —
    /// and read every tick, so it must not allocate per call. The outfield spots are still literals
    /// here: outfield depth is a per-park number and belongs with the park, not with the shared
    /// infield.
    /// </summary>
    public static IReadOnlyDictionary<string, (double X, double Z)> Positions =>
        _positions ??= new Dictionary<string, (double X, double Z)>
        {
            ["P"] = Rubber,
            ["C"] = (0, HomeSet.CatcherZ),
            ["1B"] = (78, 72),
            ["2B"] = (42, 118),
            ["3B"] = (-78, 72),
            ["SS"] = (-42, 118),
            ["LF"] = (-110, 250),
            ["CF"] = (0, 305),
            ["RF"] = (110, 250)
        };

    public static readonly string[] Order = ["P", "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF"];

    public static double Dist(double x1, double z1, double x2, double z2)
    {
        var dx = x1 - x2;
        var dz = z1 - z2;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public static (double X, double Z) Bag(int bag) => bag switch
    {
        1 => First,
        2 => Second,
        3 => Third,
        _ => Home
    };
}
