namespace GrandSluggers.Sim;

/// <summary>
/// Pairwise chemistry. Same faction is good unless rivaled; authored buddies/rivals win.
/// Starting stars come from the roster's average affinity with the captain — the Sluggers draft puzzle.
/// </summary>
public sealed class ChemistryTable
{
    readonly Dictionary<string, string> _faction = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _good = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _bad = new(StringComparer.OrdinalIgnoreCase);
    readonly RulesTable _rules;

    public ChemistryTable(IEnumerable<Character> roster, ChemistryOverrides overrides, RulesTable? rules = null)
    {
        _rules = Rules.Or(rules);
        foreach (var c in roster)
            _faction[c.Id] = c.Faction;

        foreach (var pair in overrides.Buddies)
            if (pair.Length >= 2)
                _good.Add(Key(pair[0], pair[1]));

        foreach (var pair in overrides.Rivals)
            if (pair.Length >= 2)
                _bad.Add(Key(pair[0], pair[1]));
    }

    public Chemistry Between(string a, string b)
    {
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return Chemistry.Neutral;

        var key = Key(a, b);
        if (_bad.Contains(key))
            return Chemistry.Bad;
        if (_good.Contains(key))
            return Chemistry.Good;

        if (_faction.TryGetValue(a, out var fa) &&
            _faction.TryGetValue(b, out var fb) &&
            fa.Equals(fb, StringComparison.OrdinalIgnoreCase))
            return Chemistry.Good;

        return Chemistry.Neutral;
    }

    public Chemistry Between(Character a, Character b) => Between(a.Id, b.Id);

    /// <summary>Affinity score of a pairing (stars.starting.*Score).</summary>
    public static double Score(Chemistry c, RulesTable? rules = null)
    {
        var st = Rules.Or(rules).Stars.Starting;
        return c switch
        {
            Chemistry.Good => st.GoodScore,
            Chemistry.Neutral => st.NeutralScore,
            Chemistry.Bad => st.BadScore,
            _ => st.NeutralScore
        };
    }

    /// <summary>Average chemistry of everyone except the captain, with the captain.</summary>
    public double AverageWithCaptain(Character captain, IEnumerable<Character> mates)
    {
        var others = mates
            .Where(c => !c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (others.Count == 0)
            return Score(Chemistry.Neutral, _rules);
        return others.Average(c => Score(Between(captain, c), _rules));
    }

    public double AverageWithCaptain(Team team) => AverageWithCaptain(team.Captain, team.Roster);

    /// <summary>Starting meter from the roster's affinity with the captain (stars.starting).</summary>
    public int StartingStars(Character captain, IEnumerable<Character> mates)
    {
        var st = _rules.Stars.Starting;
        var avg = AverageWithCaptain(captain, mates);
        if (avg >= st.FiveAt) return 5;
        if (avg >= st.FourAt) return 4;
        if (avg >= st.ThreeAt) return 3;
        if (avg >= st.TwoAt) return 2;
        if (avg > 0) return 1;
        return 0;
    }

    public int StartingStars(Team team) => StartingStars(team.Captain, team.Roster);

    /// <summary>Throw pair chemistry. Trails read this: good gold/purple, bad muddy and off-line.</summary>
    public Chemistry ThrowChemistry(Character from, Character to) => Between(from, to);

    /// <summary>Throw pair chemistry → speed, error roll, lateral miss (fielding.chem). Spec §8.5 reworks the roll into accuracy (P4).</summary>
    public ThrowResult FieldingThrow(Character from, Character to, Random rng, double? errorChanceWhenBad = null)
    {
        var chem = _rules.Fielding.Chem;
        var rel = ThrowChemistry(from, to);
        return rel switch
        {
            Chemistry.Good => new ThrowResult(rel, chem.GoodSpeedMul, false, 0),
            Chemistry.Bad => new ThrowResult(rel, chem.BadSpeedMul, rng.NextDouble() < (errorChanceWhenBad ?? chem.BadErrorChance), chem.BadLateralFt),
            _ => new ThrowResult(rel, 1.0, false, chem.NeutralLateralFt)
        };
    }

    /// <summary>Buddies on base multiply exit velocity (batting.buddiesOnBase).</summary>
    public double ChargePowerMul(Character batter, IEnumerable<Character> runnersOn)
    {
        var b = _rules.Batting.BuddiesOnBase;
        var buddies = runnersOn.Count(r => Between(batter, r) == Chemistry.Good);
        return buddies switch
        {
            >= 3 => b.ThreeMul,
            2 => b.TwoMul,
            1 => b.OneMul,
            _ => 1.0
        };
    }

    public bool ChemistryItemOffered(Character batter, Character? onDeck) =>
        onDeck is not null && Between(batter, onDeck) == Chemistry.Good;

    static string Key(string a, string b)
    {
        var x = a.Trim().ToLowerInvariant();
        var y = b.Trim().ToLowerInvariant();
        return string.CompareOrdinal(x, y) < 0 ? x + "|" + y : y + "|" + x;
    }
}
