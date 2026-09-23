namespace GrandSluggers.Sim;

public sealed partial class Match
{
    /// <summary>Only a settled SET defense can be rearranged. Pitcher commitment is never cancelled by a menu.</summary>
    public bool CanArrangeDefense => !Over && !Paused && !LivePlay.Active && !PitchSetup.Committed;

    /// <summary>Trade two gloves without changing batting order or stamina. A mound change uses the half's pitcher swap.</summary>
    public bool SwapDefensePositions(string first, string second)
    {
        if (!CanArrangeDefense || first == second || !Diamond.Order.Contains(first) || !Diamond.Order.Contains(second)) return false;
        var map = FieldingResolver.Assign(DefenseRoster, Pitcher, Defense.Gloves);
        if (!map.TryGetValue(first, out var a) || !map.TryGetValue(second, out var b)) return false;
        var mound = first == "P" || second == "P";
        if (mound && !SwapPitcher(first == "P" ? b : a)) return false;
        (map[first], map[second]) = (b, a);
        // Publish a new match-owned snapshot; never mutate a shared preset or its batting roster.
        if (Top) Home = Home with { Gloves = map };
        else Away = Away with { Gloves = map };
        var defense = Top ? _homeDefense : _awayDefense;
        defense.Clear();
        foreach (var pos in Diamond.Order) defense.Add(map[pos]);
        return true;
    }
}
