namespace GrandSluggers.Sim;

/// <summary>The stick on a runner during SET or the pitch.</summary>
public enum RunStick { None, Steal, Return }

/// <summary>
/// Named-bag running. Same diamond as throws (right 1B, up 2B, left 3B, down home).
/// There are no leads (D1): a runner stands on the bag until contact, a steal break, or a send.
/// A steal is armed toward the next bag, home included (D10); after the pitch it is a live ball
/// with the catcher's throw and the tag at the bag (§11.3), not a sim roll.
/// </summary>
public static class Baserunning
{
    /// <summary>Right 1B, up 2B, left 3B, down home. Dead stick is 0.</summary>
    public static int DiamondBag(double x, double y, double? mag2 = null, RulesTable? rules = null) =>
        InPlay.DiamondBag(x, y, mag2, rules);

    public static int NextBag(int bag) => bag is >= 1 and <= 3 ? bag + 1 : 0;

    public static int PrevBag(int bag) => bag switch
    {
        1 => 4,
        2 => 1,
        3 => 2,
        _ => 0
    };

    /// <summary>Next bag for a steal: second, third, or home (D10). 0 for an invalid bag.</summary>
    public static int StealTarget(int fromBag) => fromBag is >= 1 and <= 3 ? fromBag + 1 : 0;

    /// <summary>
    /// What the stick says about the selected runner before the ball is in play (spec §9.2, §11.1):
    /// toward the next bag arms a steal (same as L3); toward this bag or the one behind returns
    /// (and cancels the steal). There is no lead stick.
    /// </summary>
    public static RunStick StickVerb(int stickBag, int selectedBag)
    {
        if (stickBag <= 0 || selectedBag is < 1 or > 3) return RunStick.None;
        if (stickBag == NextBag(selectedBag)) return RunStick.Steal;
        if (stickBag == selectedBag || stickBag == PrevBag(selectedBag)) return RunStick.Return;
        return RunStick.None;
    }

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
            4 => false,
            _ => true
        };

    /// <summary>
    /// A steal is offered from an occupied bag toward an open next bag, or toward a bag whose runner
    /// is armed too (the double steal, §11.1).
    /// </summary>
    public static bool CanSteal(int fromBag, bool first, bool second, bool third, bool nextRunnerArmed = false) =>
        StealTarget(fromBag) > 0
        && Occupied(fromBag, first, second, third)
        && (!NextOccupied(fromBag, first, second, third) || nextRunnerArmed);

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
}
