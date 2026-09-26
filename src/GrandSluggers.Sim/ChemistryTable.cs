namespace GrandSluggers.Sim;

/// <summary>
/// Pairwise chemistry (WD-28). In order: an authored story pair (overrides.json); a shared crew is good; faction-mates are
/// good; a crew and its rival crew are bad; otherwise neutral. <see cref="Reason(string, string)"/> names the rule.
/// Chemistry pays off in the field only (§8.5, §12): it does not set starting Stars (PH-16-R16), which are the
/// same for both teams (<see cref="StarRules.StartingReserve"/>).
/// </summary>
public sealed class ChemistryTable
{
    readonly Dictionary<string, string> _faction = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _good = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _bad = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, IReadOnlyList<string>> _crews = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _rivalCrews = new(StringComparer.Ordinal);
    readonly RulesTable _rules;

    public ChemistryTable(IEnumerable<Character> roster, ChemistryOverrides overrides, RulesTable rules, IEnumerable<Crew>? crews = null)
    {
        _rules = rules;
        foreach (var c in roster)
        {
            _faction[c.Id] = c.Faction;
            _crews[c.Id] = c.Crews;
        }
        foreach (var crew in crews ?? [])
            if (crew.Rival is { } r)
            {
                _rivalCrews.Add(crew.Id + "|" + r);
                _rivalCrews.Add(r + "|" + crew.Id);
            }

        foreach (var pair in overrides.Buddies)
            if (pair.Length >= 2)
                _good.Add(Key(pair[0], pair[1]));

        foreach (var pair in overrides.Rivals)
            if (pair.Length >= 2)
                _bad.Add(Key(pair[0], pair[1]));
    }

    public Chemistry Between(string a, string b) => Reason(a, b).Chemistry;

    public Chemistry Between(Character a, Character b) => Between(a.Id, b.Id);

    /// <summary>
    /// Why two players have the chemistry they have (WD-28): the first rule in order that decides it, and the crews it
    /// names. <see cref="Between(string, string)"/> is this function's verdict, so the lineup, the book and the field never
    /// disagree about a pair.
    /// </summary>
    public ChemistryReason Reason(string a, string b)
    {
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return new(Chemistry.Neutral, ChemistryWhy.None);

        var key = Key(a, b);
        if (_bad.Contains(key))
            return new(Chemistry.Bad, ChemistryWhy.StoryPair);
        if (_good.Contains(key))
            return new(Chemistry.Good, ChemistryWhy.StoryPair);

        var ca = _crews.GetValueOrDefault(a) ?? [];
        var cb = _crews.GetValueOrDefault(b) ?? [];
        foreach (var x in ca)
            if (cb.Contains(x))
                return new(Chemistry.Good, ChemistryWhy.SharedCrew, x);

        if (_faction.TryGetValue(a, out var fa) &&
            _faction.TryGetValue(b, out var fb) &&
            fa.Equals(fb, StringComparison.OrdinalIgnoreCase))
            return new(Chemistry.Good, ChemistryWhy.FactionMates);

        foreach (var x in ca)
        foreach (var y in cb)
            if (_rivalCrews.Contains(x + "|" + y))
                return new(Chemistry.Bad, ChemistryWhy.RivalCrews, x, y);

        return new(Chemistry.Neutral, ChemistryWhy.None);
    }

    public ChemistryReason Reason(Character a, Character b) => Reason(a.Id, b.Id);

    /// <summary>Throw pair chemistry. Trails read this: good gold/purple, bad muddy and off-line.</summary>
    public Chemistry ThrowChemistry(Character from, Character to) => Between(from, to);

    /// <summary>
    /// Throw pair chemistry → the throw's input (§8.5, fielding.chem, fielding.throw): good is
    /// faster; bad is slanted with slantChance (slower, a lateral miss of slantLateral ft to one
    /// side) and flies at badSpeedMul otherwise — as shipped, 0.90 with no slant
    /// (F693-03-negative-chemistry, #722). Every throw carries the thrower's lateral
    /// error, σ = (11 − Arm) × lateralSigmaPerFieldDeficitFt. The roll is on the input; the
    /// receiver's radius decides the catch where the ball lands.
    /// </summary>
    public ThrowResult FieldingThrow(Character from, Character to, Random rng)
    {
        var chem = _rules.Fielding.Chem;
        var thr = _rules.Fielding.Throw;
        var rel = ThrowChemistry(from, to);
        var sigma = Math.Max(0, 11 - from.Stats.Arm) * thr.LateralSigmaPerFieldDeficitFt;
        var lateral = Gauss(rng) * sigma;
        if (rel == Chemistry.Bad && rng.NextDouble() < chem.SlantChance)
        {
            var miss = chem.SlantLateralMinFt + rng.NextDouble() * (chem.SlantLateralMaxFt - chem.SlantLateralMinFt);
            var side = rng.NextDouble() < 0.5 ? -1 : 1;
            return new ThrowResult(rel, chem.SlantSpeedMul, true, side * miss);
        }
        var speed = rel switch { Chemistry.Good => chem.GoodSpeedMul, Chemistry.Bad => chem.BadSpeedMul, _ => 1.0 };
        return new ThrowResult(rel, speed, false, lateral);
    }

    static double Gauss(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// The on-deck item offer (spec §12). Jack removed it (PH-16-R15, #891): no pair offers an item,
    /// so this is always false. The items themselves stay dormant, not deleted — <see cref="ErrorItems"/>,
    /// <c>batting.items</c>, <see cref="Match.ApplyOffenseItem"/> / <see cref="Match.ThrowItem"/>, the
    /// tutorial item objective and the clients' item controls — and wake only when a new item source
    /// sets <see cref="AtBatResult.ChemistryItemOffered"/>. The signature is kept so those readers compile.
    /// </summary>
    public bool ChemistryItemOffered(Character batter, Character? onDeck) => false;

    static string Key(string a, string b)
    {
        var x = a.Trim().ToLowerInvariant();
        var y = b.Trim().ToLowerInvariant();
        return string.CompareOrdinal(x, y) < 0 ? x + "|" + y : y + "|" + x;
    }
}

/// <summary>The rule that decides a pair's chemistry (WD-28), in the order <see cref="ChemistryTable.Reason(string, string)"/> reads them.</summary>
public enum ChemistryWhy
{
    /// <summary>Nothing links them: neutral.</summary>
    None,
    /// <summary>An authored story pair (data/chemistry/overrides.json), good or bad; it beats every other rule.</summary>
    StoryPair,
    /// <summary>They share a crew: good, whatever their teams.</summary>
    SharedCrew,
    /// <summary>They come from the same home team (faction): good.</summary>
    FactionMates,
    /// <summary>One's crew is the other's rival crew: bad.</summary>
    RivalCrews,
}

/// <summary>
/// A pair's chemistry and why (<see cref="ChemistryTable.Reason(string, string)"/>): the verdict, the rule, and the crew ids
/// the rule names — the shared crew, or the first player's crew and the second's rival crew.
/// </summary>
public readonly record struct ChemistryReason(Chemistry Chemistry, ChemistryWhy Why, string? Crew = null, string? RivalCrew = null);
