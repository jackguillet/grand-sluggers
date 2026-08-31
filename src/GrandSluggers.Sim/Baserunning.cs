namespace GrandSluggers.Sim;

/// <summary>
/// Named-bag running. Same diamond as throws (right 1B, up 2B, left 3B, down home).
/// Home is never a steal target. Per-bag lead lives on <see cref="RunnerState"/>.
/// </summary>
public static class Baserunning
{
    /// <summary>Right 1B, up 2B, left 3B, down home. Dead stick is 0.</summary>
    public static int DiamondBag(double x, double y, double mag2 = 0.55) =>
        InPlay.DiamondBag(x, y, mag2);

    public static int NextBag(int bag) => bag is >= 1 and <= 3 ? bag + 1 : 0;

    public static int PrevBag(int bag) => bag switch
    {
        1 => 4,
        2 => 1,
        3 => 2,
        _ => 0
    };

    /// <summary>Next bag for a steal. 0 means no steal (home or invalid).</summary>
    public static int StealTarget(int fromBag) => fromBag is 1 or 2 ? fromBag + 1 : 0;

    public static bool Occupied(int bag, bool first, bool second, bool third) => bag switch
    {
        1 => first,
        2 => second,
        3 => third,
        _ => false
    };

    public static bool CanSelect(int bag, bool first, bool second, bool third) =>
        Occupied(bag, first, second, third);

    public static bool NextOccupied(int fromBag, bool first, bool second, bool third) =>
        StealTarget(fromBag) switch
        {
            2 => second,
            3 => third,
            _ => true
        };

    public static bool CanSteal(int fromBag, bool first, bool second, bool third) =>
        StealTarget(fromBag) > 0
        && Occupied(fromBag, first, second, third)
        && !NextOccupied(fromBag, first, second, third);

    /// <summary>
    /// Keep a pad-named runner while they occupy. Otherwise snap to the lead runner.
    /// </summary>
    public static int SyncSelected(int selected, bool picked, bool first, bool second, bool third, int leadBag)
    {
        if (picked && Occupied(selected, first, second, third)) return selected;
        return leadBag is 1 or 2 or 3 ? leadBag : 0;
    }

    /// <summary>Mini-diamond UV. U 0=3B 1=1B. V 0=home 1=second. Same map as throw tells.</summary>
    public static (double U, double V) DiamondPip(int bag) => bag switch
    {
        1 => (1.0, 0.5),
        2 => (0.5, 1.0),
        3 => (0.0, 0.5),
        4 => (0.5, 0.0),
        _ => (0.5, 0.5)
    };

    /// <summary>Occupied pip walks toward the next bag. Lead 1.0 is 45% of the way.</summary>
    public static (double U, double V) MiniLead(int bag, double lead01)
    {
        var from = DiamondPip(bag);
        var to = DiamondPip(NextBag(bag));
        var t = Math.Clamp(lead01, 0, 1) * 0.45;
        return (from.U + (to.U - from.U) * t, from.V + (to.V - from.V) * t);
    }
}
