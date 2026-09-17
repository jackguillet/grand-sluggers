namespace GrandSluggers.Sim;

/// <summary>
/// Chemistry stickers and jumping stars. LineupScreens is the two-screen draft;
/// hearts / scribbles still mark buddies and rivals vs the captain.
/// </summary>
public static class ChemistryToy
{
    public const string Heart = "heart";
    public const string Scribble = "scribble";
    public const string None = "none";

    /// <summary>1B 3/4 on the huddle. Not bird's-eye high-home.</summary>
    public const double CamX = 14.0;
    public const double CamY = 9.5;
    public const double CamZ = -8.0;
    public const double LookX = 0.4;
    public const double LookY = 2.4;
    public const double LookZ = 20.0;
    public const double Fov = 50;

    /// <summary>Compact diamond on the infield dirt so nine toys fill the 3/4.</summary>
    public const double ToySpanX = 18;
    public const double ToySpanZ = 26;
    public const double ToyHomeZ = 8;
    public const double HeartY = 3.2;
    public const double HighlightX = 2.4;
    public const double HighlightZ = -2.8;

    public static string Sticker(Chemistry chem) => chem switch
    {
        Chemistry.Good => Heart,
        Chemistry.Bad => Scribble,
        _ => None
    };

    /// <summary>
    /// Mini-diamond UV. U is the 3B (−) to 1B (+) axis, V is home (0) toward CF (1).
    ///
    /// <para>
    /// The frame is the outfield itself — the right fielder's x and the centre fielder's depth,
    /// read from <see cref="Diamond.Positions"/>. Before #725 those were the shipped numbers typed
    /// here; once the starts became data a root that moves them would have squashed every token
    /// toward the middle of a toy diamond that had not moved.
    /// </para>
    /// </summary>
    public static (double U, double V) MiniSpot(string pos)
    {
        if (!Diamond.Positions.TryGetValue(pos, out var p)) return (0, 0.35);
        return (p.X / Diamond.Positions["RF"].X, p.Z / Diamond.Positions["CF"].Z);
    }

    /// <summary>
    /// The V a whole group's token sits at, for the draft screen's group rows. These are a fixed
    /// layout, not a reading of <see cref="Diamond.Positions"/>: they were authored from the shipped
    /// depths and they do not follow a data root that moves the starts (#725). What V should mean
    /// under a trial — the live depth, or a layout that stays put — is a design call nobody has made.
    /// </summary>
    public static (double U, double V) GroupTokenSpot(string group) => group switch
    {
        "P" => (0, 0.20),
        "C" => (0, 0.00),
        "IF" => (0, 0.39),
        "OF" => (0, 0.82),
        _ => (0, 0.40)
    };

    /// <summary>World feet on the compact diamond. CF is in front of the real wall.</summary>
    public static (double X, double Z) WorldSpot(string pos)
    {
        var uv = MiniSpot(pos);
        return (uv.U * ToySpanX, ToyHomeZ + uv.V * ToySpanZ);
    }

    public static (double X, double Y, double Z) HeartSpot(
        (double X, double Z) a, (double X, double Z) b) =>
        ((a.X + b.X) * 0.5, HeartY, (a.Z + b.Z) * 0.5);

    /// <summary>Filled stars bounce. Empty stars stay small.</summary>
    public static double StarScale(int index, int filled, double t)
    {
        var on = index >= 0 && index < filled;
        if (!on) return 0.72;
        return 1.0 + 0.14 * Math.Sin(t * 6.2 + index * 0.9);
    }

    public static bool StarFilled(int index, int filled) => index >= 0 && index < filled;

    public static bool CameraIsThreeQuarter(double x, double y, double z) =>
        x > 8 && z < 0 && z > -20 && y < 16 && y > 6;
}
