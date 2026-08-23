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

    public ChemistryTable(IEnumerable<Character> roster, ChemistryOverrides overrides)
    {
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

    public static int Score(Chemistry c) => c switch
    {
        Chemistry.Good => 100,
        Chemistry.Neutral => 50,
        Chemistry.Bad => 10,
        _ => 50
    };

    /// <summary>Average chemistry of everyone except the captain, with the captain.</summary>
    public double AverageWithCaptain(Team team)
    {
        var others = team.Roster.Where(c => !c.Id.Equals(team.Captain.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        if (others.Count == 0)
            return Score(Chemistry.Neutral);
        return others.Average(c => Score(Between(team.Captain, c)));
    }

    public int StartingStars(Team team)
    {
        var avg = AverageWithCaptain(team);
        if (avg >= 70) return 5;
        if (avg >= 55) return 4;
        if (avg >= 35) return 3;
        if (avg >= 15) return 2;
        if (avg > 0) return 1;
        return 0;
    }

    /// <summary>Throw pair chemistry. Trails read this: good gold/purple, bad muddy and off-line.</summary>
    public Chemistry ThrowChemistry(Character from, Character to) => Between(from, to);

    public ThrowResult FieldingThrow(Character from, Character to, Random rng, double errorChanceWhenBad = 0.25)
    {
        var rel = ThrowChemistry(from, to);
        return rel switch
        {
            Chemistry.Good => new ThrowResult(rel, 1.35, false, 0),
            Chemistry.Bad => new ThrowResult(rel, 0.70, rng.NextDouble() < errorChanceWhenBad, 14),
            _ => new ThrowResult(rel, 1.0, false, 3)
        };
    }

    public double ChargePowerMul(Character batter, IEnumerable<Character> runnersOn)
    {
        var buddies = runnersOn.Count(r => Between(batter, r) == Chemistry.Good);
        return buddies switch
        {
            >= 3 => 1.50,
            2 => 1.25,
            1 => 1.10,
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
