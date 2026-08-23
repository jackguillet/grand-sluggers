namespace GrandSluggers.Sim;

/// <summary>
/// Session Challenge: pick a captain, beat rivals, recruit one opponent role player per win.
/// No disk save — this is the two-day fun check.
/// </summary>
public sealed class Challenge
{
    public string CaptainId { get; }
    public HashSet<string> Owned { get; }
    public HashSet<string> Beaten { get; }
    public Character? LastRecruit { get; private set; }
    public bool LastWin { get; private set; }

    Challenge(string captainId, HashSet<string> owned)
    {
        CaptainId = captainId;
        Owned = owned;
        Beaten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static Challenge Start(ContentCatalog content, string captainId)
    {
        var cap = content.Must(captainId);
        var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { cap.Id };
        foreach (var c in content.Characters.Values)
        {
            if (!c.Captain && c.Faction.Equals(cap.Faction, StringComparison.OrdinalIgnoreCase))
                owned.Add(c.Id);
        }
        return new Challenge(cap.Id, owned);
    }

    public string NextOpponentId(ContentCatalog content)
    {
        var me = content.Must(CaptainId);
        for (var i = PresetTeams.CaptainIds.Length - 1; i >= 0; i--)
        {
            var id = PresetTeams.CaptainIds[i];
            if (id.Equals(CaptainId, StringComparison.OrdinalIgnoreCase) || Beaten.Contains(id))
                continue;
            if (content.Chemistry.Between(me.Id, id) == Chemistry.Bad)
                return id;
        }
        foreach (var id in PresetTeams.CaptainIds)
        {
            if (id.Equals(CaptainId, StringComparison.OrdinalIgnoreCase) || Beaten.Contains(id))
                continue;
            return id;
        }
        return PresetTeams.NextCaptain(CaptainId);
    }

    public bool AllBeaten
    {
        get
        {
            var others = PresetTeams.CaptainIds.Count(id => !id.Equals(CaptainId, StringComparison.OrdinalIgnoreCase));
            return Beaten.Count >= others;
        }
    }

    public Match MakeMatch(ContentCatalog content, int innings = Match.DefaultInnings, int seed = 1, string? parkId = null)
    {
        var opp = NextOpponentId(content);
        var (home, away) = PresetTeams.Pair(content, CaptainId, opp, Owned);
        parkId ??= PresetTeams.HomeParkId(opp);
        if (!content.Parks.TryGetValue(parkId, out var park))
            park = content.Parks["harbor-diamond"];
        return new Match(content, away, home, park, innings, seed);
    }

    public Character? Resolve(Match match) =>
        ApplyOutcome(match.HomeScore > match.AwayScore, match.Away.Captain, match.Away.Roster, match.Mvp().Who);

    public Character? ApplyOutcome(
        bool won,
        Character awayCaptain,
        IEnumerable<Character> awayRoster,
        Character? mvpWho)
    {
        LastWin = won;
        LastRecruit = null;
        if (!won) return null;

        Beaten.Add(awayCaptain.Id);
        var pool = awayRoster
            .Where(c => !c.Captain && !Owned.Contains(c.Id))
            .ToList();
        if (pool.Count == 0) return null;

        var pick = mvpWho is not null && pool.Any(c => c.Id.Equals(mvpWho.Id, StringComparison.OrdinalIgnoreCase))
            ? mvpWho
            : pool[0];
        Owned.Add(pick.Id);
        LastRecruit = pick;
        return pick;
    }
}
