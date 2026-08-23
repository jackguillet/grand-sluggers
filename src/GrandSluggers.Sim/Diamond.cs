namespace GrandSluggers.Sim;

/// <summary>Feet. Home at origin, +Z toward second/center, +X toward first.</summary>
public static class Diamond
{
    public const double Baseline = 90;
    public const double Mound = 60.5;
    public static readonly (double X, double Z) Home = (0, 0);
    public static readonly (double X, double Z) First = (63.64, 63.64);
    public static readonly (double X, double Z) Second = (0, 127.28);
    public static readonly (double X, double Z) Third = (-63.64, 63.64);
    public static readonly (double X, double Z) Rubber = (0, Mound);

    public static readonly IReadOnlyDictionary<string, (double X, double Z)> Positions =
        new Dictionary<string, (double X, double Z)>
        {
            ["P"] = Rubber,
            ["C"] = (0, -4),
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
}
