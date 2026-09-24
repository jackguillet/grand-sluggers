namespace GrandSluggers.Sim;

/// <summary>
/// Arrange defense in SET: the player trades a named pair of non-pitcher gloves through the match's own
/// <see cref="Match.SwapDefensePositions"/>. Browsing, a first pick and cancel never reach this command, so they earn nothing.
/// </summary>
public sealed partial class TutorialSession
{
    /// <summary>The two positions the lesson names, or empty outside the defense-swap policy.</summary>
    public IReadOnlyList<string> SwapPair => _setup.SwapPair ?? [];
    /// <summary>Every trade in the window, the mound included, routes through <see cref="SwapPositions(string, string, out bool, LivePlayCommandSource)"/>.</summary>
    public bool IsDefenseSwapLesson => _setup.Policy == "defense-swap";
    /// <summary>The teaching nine is the away team: the lesson is staged in the bottom half, so player 1 sits the away seat.</summary>
    public bool DefendsAsAway => _setup.Bottom;

    void PrepareBottomHalf()
    {
        // Three ordinary called strikeouts end the top half; the away nine then takes the field in SET.
        var strike = new PitchCommand(PitchFamily.Fastball, 0, false);
        for (var pitch = 0; pitch < 9 && Match.Top && !Match.Over; pitch++)
            Match.BeginAtBat(strike, Take, out _, out _);
        if (Match.Top || Match.Inning != 1 || Match.Outs != 0 || Match.LivePlay.Active)
            throw new InvalidDataException("Tutorial bottom-half setup no longer produces three called strikeouts.");
    }

    public bool SwapPositions(string first, string second, LivePlayCommandSource source = LivePlayCommandSource.Human) =>
        SwapPositions(first, second, out _, source);

    /// <summary>
    /// The window's trade. Accepted (and recorded) whenever the match could be asked; <paramref name="changed"/> is the
    /// match's own answer. Only a human, non-demonstration trade of exactly the named pair, with the batting order, arm
    /// pools, pitcher and every other glove kept, earns the attempt.
    /// </summary>
    public bool SwapPositions(string first, string second, out bool changed, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        changed = false;
        if (!Accepts(source) || !IsDefenseSwapLesson || first == second || !Diamond.Order.Contains(first)
            || !Diamond.Order.Contains(second) || !Match.CanArrangeDefense) return false;
        var before = Gloves();
        var order = BattingOrder();
        var pools = ArmPools();
        var pitcher = Match.Pitcher.Id;
        var offense = Match.Offense;
        _inputs.Add(new(Elapsed, source, SwapPositions: [first, second]));
        changed = Match.SwapDefensePositions(first, second);
        var after = Gloves();
        var traded = changed && after[first] == before[second] && after[second] == before[first]
            && Diamond.Order.Where(p => p != first && p != second).All(p => after[p] == before[p]);
        var kept = order.SequenceEqual(BattingOrder()) && pools.SequenceEqual(ArmPools())
            && Match.Pitcher.Id == pitcher && ReferenceEquals(offense, Match.Offense);
        var named = SwapPair.Count == 2 && SwapPair.Contains(first) && SwapPair.Contains(second);
        var mound = first == "P" || second == "P";
        var earned = source == LivePlayCommandSource.Human && !Demonstration && named && traded && kept;
        var code = earned ? "positions-traded" : mound ? "pitcher-change" : !traded ? "swap-refused" : !named ? "wrong-pair" : "swap-missed";
        Finish(earned, code, code switch
        {
            "positions-traded" => $"You traded {SwapPair[0]} and {SwapPair[1]}. The batting order and every arm stayed the same.",
            "pitcher-change" => $"That moved the pitcher. Leave the mound alone and trade {SwapPair[0]} and {SwapPair[1]}.",
            "wrong-pair" => $"That traded {first} and {second}. Pick {SwapPair[0]}, then {SwapPair[1]}.",
            _ => $"The trade did not happen. Before the pitch charge, pick {SwapPair[0]}, then {SwapPair[1]}.",
        });
        return true;
    }

    Dictionary<string, string> Gloves() => FieldingResolver.Assign(Match.DefenseRoster, Match.Pitcher, Match.Defense.Gloves)
        .ToDictionary(p => p.Key, p => p.Value.Id);

    // Keyed by player, not by glove slot: the trade reorders the defense list itself.
    (string, int)[] ArmPools() => Match.DefenseRoster.Select(c => (c.Id, Match.StaminaOf(c))).OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();

    string[] BattingOrder() => (Match.Top ? Match.HomeOrder : Match.AwayOrder).Select(c => c.Id).ToArray();
}
