namespace GrandSluggers.Sim;

/// <summary>
/// Lineup as a chemistry toy: mini diamond, sticker edges, jumping stars.
/// HUD draws this; tests lock the layout.
/// </summary>
public static class ChemistryToy
{
    public const string Heart = "heart";
    public const string Scribble = "scribble";
    public const string None = "none";

    public static string Sticker(Chemistry chem) => chem switch
    {
        Chemistry.Good => Heart,
        Chemistry.Bad => Scribble,
        _ => None
    };

    /// <summary>Mini-diamond UV. U is 3B (−) to 1B (+). V is home (0) toward CF (1).</summary>
    public static (double U, double V) MiniSpot(string pos)
    {
        if (!Diamond.Positions.TryGetValue(pos, out var p)) return (0, 0.35);
        return (p.X / 110.0, p.Z / 305.0);
    }

    /// <summary>World-space mini diamond in front of the lineup camera. Not the 90-ft field.</summary>
    public static (double X, double Z) LineupWorldSpot(string pos)
    {
        var uv = MiniSpot(pos);
        return (uv.U * 16.0, 3.0 + uv.V * 24.0);
    }

    public static (double U, double V) GroupTokenSpot(string group) => group switch
    {
        "P" => (0, 0.20),
        "C" => (0, 0.00),
        "IF" => (0, 0.39),
        "OF" => (0, 0.82),
        _ => (0, 0.40)
    };

    /// <summary>Filled stars bounce. Empty stars stay small.</summary>
    public static double StarScale(int index, int filled, double t)
    {
        var on = index >= 0 && index < filled;
        if (!on) return 0.72;
        return 1.0 + 0.14 * Math.Sin(t * 6.2 + index * 0.9);
    }

    public static bool StarFilled(int index, int filled) => index >= 0 && index < filled;
}
