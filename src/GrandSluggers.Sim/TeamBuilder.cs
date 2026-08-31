namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition draft: nine names, batting order, gloves. Starting stars are chemistry with the captain.
/// </summary>
public sealed class TeamBuilder
{
    public const int Size = 9;
    public static readonly string[] GloveGroups = ["P", "C", "IF", "OF"];
    public static readonly string[] Infield = ["1B", "2B", "3B", "SS"];
    public static readonly string[] Outfield = ["LF", "CF", "RF"];

    readonly ContentCatalog _content;
    readonly List<Character> _order = [];
    readonly Dictionary<string, Character> _glove = new(StringComparer.OrdinalIgnoreCase);

    public Character Captain { get; }
    public bool LockCaptain { get; }
    public string Name { get; }

    TeamBuilder(ContentCatalog content, Character captain, bool lockCaptain)
    {
        _content = content;
        Captain = captain;
        LockCaptain = lockCaptain;
        Name = PresetTeams.TeamName(captain);
    }

    public static TeamBuilder Draft(
        ContentCatalog content,
        string captainId,
        IEnumerable<string>? exclude = null,
        bool lockCaptain = true)
    {
        var filled = PresetTeams.ForCaptain(content, captainId, exclude);
        return FromRoster(content, filled.Captain, filled.Roster, lockCaptain)
            ?? throw new InvalidOperationException("draft needs nine");
    }

    /// <summary>A complete nine: batting order and gloves. LineupScreens confirms into this.</summary>
    public static TeamBuilder? FromRoster(
        ContentCatalog content,
        Character captain,
        IReadOnlyList<Character> roster,
        bool lockCaptain = true)
    {
        if (roster.Count != Size) return null;
        if (roster.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Size)
            return null;
        if (roster.All(c => !c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)))
            return null;

        var b = new TeamBuilder(content, captain, lockCaptain);
        foreach (var c in Team.DefaultBattingOrder(captain, roster))
            b._order.Add(c);
        b.AssignDefaultGloves();
        return b;
    }

    public IReadOnlyList<Character> Order => _order;
    public IReadOnlyDictionary<string, Character> Gloves => _glove;
    public int StartingStars => _content.Chemistry.StartingStars(ToTeam());
    public double AverageWithCaptain => _content.Chemistry.AverageWithCaptain(ToTeam());

    public Chemistry Chem(Character c) => _content.Chemistry.Between(Captain, c);

    public IReadOnlyList<Character> Pool(IEnumerable<string>? taken = null)
    {
        var blocked = new HashSet<string>(taken ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var c in _order)
            blocked.Add(c.Id);
        return _content.Characters.Values
            .Where(c => !blocked.Contains(c.Id))
            .OrderByDescending(c => c.Captain)
            .ThenBy(c => c.Faction, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Replace the eight around the captain. Captain stays. Exhibition can pick anyone.</summary>
    public bool Fill(IEnumerable<string> mateIds)
    {
        var mates = mateIds.ToList();
        if (mates.Count != Size - 1) return false;
        if (mates.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mates.Count) return false;

        var people = new List<Character>(Size) { Captain };
        foreach (var id in mates)
        {
            if (id.Equals(Captain.Id, StringComparison.OrdinalIgnoreCase)) return false;
            if (!_content.Characters.TryGetValue(id, out var c)) return false;
            people.Add(c);
        }

        _order.Clear();
        foreach (var c in Team.DefaultBattingOrder(Captain, people))
            _order.Add(c);
        AssignDefaultGloves();
        return true;
    }

    /// <summary>Swap a non-captain (or any slot if unlocked) for someone off the roster.</summary>
    public bool Replace(string outgoingId, string incomingId)
    {
        if (outgoingId.Equals(incomingId, StringComparison.OrdinalIgnoreCase)) return false;
        if (LockCaptain && outgoingId.Equals(Captain.Id, StringComparison.OrdinalIgnoreCase)) return false;
        if (!_content.Characters.TryGetValue(incomingId, out var incoming)) return false;
        if (_order.Any(c => c.Id.Equals(incoming.Id, StringComparison.OrdinalIgnoreCase))) return false;

        var i = _order.FindIndex(c => c.Id.Equals(outgoingId, StringComparison.OrdinalIgnoreCase));
        if (i < 0) return false;

        var outgoing = _order[i];
        _order[i] = incoming;
        foreach (var pos in Diamond.Order)
        {
            if (_glove.TryGetValue(pos, out var who) &&
                who.Id.Equals(outgoing.Id, StringComparison.OrdinalIgnoreCase))
            {
                _glove[pos] = incoming;
                break;
            }
        }
        return true;
    }

    public bool SwapOrder(int a, int b)
    {
        if (a < 0 || b < 0 || a >= _order.Count || b >= _order.Count || a == b) return false;
        (_order[a], _order[b]) = (_order[b], _order[a]);
        return true;
    }

    /// <summary>Give this roster player a glove, swapping with whoever holds it.</summary>
    public bool SetGlove(string pos, string whoId)
    {
        pos = NormalizePos(pos);
        if (!Diamond.Positions.ContainsKey(pos)) return false;
        var who = _order.FirstOrDefault(c => c.Id.Equals(whoId, StringComparison.OrdinalIgnoreCase));
        if (who is null) return false;
        var current = PosOf(whoId);
        if (current is null) return false;
        if (current.Equals(pos, StringComparison.OrdinalIgnoreCase)) return true;
        if (!_glove.TryGetValue(pos, out var occupant))
        {
            _glove[pos] = who;
            _glove.Remove(current);
            return true;
        }
        _glove[pos] = who;
        _glove[current] = occupant;
        return true;
    }

    /// <summary>Walk P → C → IF → OF → P, then around the group.</summary>
    public bool CycleGlove(string whoId)
    {
        var pos = PosOf(whoId);
        if (pos is null) return false;
        return SetGlove(NextGlove(pos), whoId);
    }

    public string? PosOf(string whoId)
    {
        foreach (var kv in _glove)
            if (kv.Value.Id.Equals(whoId, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        return null;
    }

    public static string GloveGroup(string pos) => NormalizePos(pos) switch
    {
        "P" => "P",
        "C" => "C",
        "1B" or "2B" or "3B" or "SS" => "IF",
        "LF" or "CF" or "RF" => "OF",
        _ => pos
    };

    public Team ToTeam()
    {
        var roster = new List<Character>(Size);
        foreach (var pos in Diamond.Order)
        {
            if (_glove.TryGetValue(pos, out var c) &&
                roster.All(x => !x.Id.Equals(c.Id, StringComparison.OrdinalIgnoreCase)))
                roster.Add(c);
        }
        foreach (var c in _order)
        {
            if (roster.All(x => !x.Id.Equals(c.Id, StringComparison.OrdinalIgnoreCase)))
                roster.Add(c);
        }

        var starter = _glove.TryGetValue("P", out var p) ? p : Captain;
        return new Team(Name, Captain, roster, _order.ToList(), starter);
    }

    void AssignDefaultGloves()
    {
        _glove.Clear();
        _glove["P"] = Captain;
        var rest = _order.Where(c => !c.Id.Equals(Captain.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        var i = 0;
        foreach (var pos in Diamond.Order)
        {
            if (pos == "P") continue;
            if (i >= rest.Count) break;
            _glove[pos] = rest[i++];
        }
    }

    public static string NormalizePos(string pos)
    {
        var p = pos.Trim().ToUpperInvariant();
        return p switch
        {
            "IF" => "1B",
            "OF" => "LF",
            "PITCHER" => "P",
            "CATCHER" => "C",
            _ => p
        };
    }

    static string NextGlove(string pos)
    {
        var i = Array.IndexOf(Diamond.Order, pos);
        if (i < 0) return "P";
        return Diamond.Order[(i + 1) % Diamond.Order.Length];
    }
}
