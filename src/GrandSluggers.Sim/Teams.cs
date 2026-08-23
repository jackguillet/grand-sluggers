namespace GrandSluggers.Sim;

public static class PresetTeams
{
    public static readonly string[] CaptainIds = ["rio", "vale", "zig", "brondo", "konga", "ashlord"];

    public static Team SparkAllStars(ContentCatalog content) => content.Team(
        "Spark All-Stars",
        "rio", "nico", "pip", "marlow", "vale", "lace", "zig", "dart", "vine");

    public static Team EmberCourt(ContentCatalog content) => content.Team(
        "Ember Court",
        "ashlord", "cinder", "soot", "brondo", "boom", "konga", "frost", "grit", "hex");

    public static Team MixedRivals(ContentCatalog content) => content.Team(
        "Mixed Rivals",
        "rio", "ashlord", "brondo", "cinder", "vale", "boom", "konga", "soot", "frost");

    public static string NextCaptain(string captainId)
    {
        var i = IndexOfCaptain(captainId);
        return CaptainIds[(i + 1) % CaptainIds.Length];
    }

    public static string PrevCaptain(string captainId)
    {
        var i = IndexOfCaptain(captainId);
        return CaptainIds[(i - 1 + CaptainIds.Length) % CaptainIds.Length];
    }

    public static int IndexOfCaptain(string captainId)
    {
        var i = Array.FindIndex(CaptainIds, id => id.Equals(captainId, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? 0 : i;
    }

    public static string HomeParkId(string captainId) => captainId.ToLowerInvariant() switch
    {
        "vale" => "crystal-rink",
        "zig" => "funfair-park",
        "brondo" => "rooftop-city",
        "konga" => "funfair-park",
        "ashlord" => "rooftop-city",
        _ => "harbor-diamond"
    };

    public static string TeamName(Character captain) => captain.Id.ToLowerInvariant() switch
    {
        "rio" => "Spark All-Stars",
        "vale" => "Royal Rink",
        "zig" => "Carnival Crew",
        "brondo" => "Goldrush",
        "konga" => "Canopy Clan",
        "ashlord" => "Ember Court",
        _ => captain.Name
    };

    public static (Team Home, Team Away) Pair(
        ContentCatalog content,
        string homeCaptain,
        string awayCaptain,
        IEnumerable<string>? homePrefer = null)
    {
        if (homeCaptain.Equals(awayCaptain, StringComparison.OrdinalIgnoreCase))
            awayCaptain = NextCaptain(homeCaptain);
        var home = ForCaptain(content, homeCaptain, prefer: homePrefer);
        var away = ForCaptain(content, awayCaptain, exclude: home.Roster.Select(c => c.Id));
        return (home, away);
    }

    public static Team ForCaptain(
        ContentCatalog content,
        string captainId,
        IEnumerable<string>? exclude = null,
        IEnumerable<string>? prefer = null)
    {
        var cap = content.Must(captainId);
        var taken = new HashSet<string>(exclude ?? [], StringComparer.OrdinalIgnoreCase) { cap.Id };
        var roster = new List<Character> { cap };

        void TryAdd(Character c)
        {
            if (roster.Count >= 9) return;
            if (!taken.Add(c.Id)) return;
            roster.Add(c);
        }

        if (prefer is not null)
        {
            foreach (var id in prefer)
            {
                if (content.Characters.TryGetValue(id, out var c))
                    TryAdd(c);
            }
        }

        foreach (var c in content.Characters.Values
                     .Where(c => !c.Captain && c.Faction.Equals(cap.Faction, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(Tools))
            TryAdd(c);

        foreach (var c in content.Characters.Values
                     .Where(c => !c.Captain)
                     .OrderByDescending(c => FillScore(content, cap, c)))
            TryAdd(c);

        foreach (var c in content.Characters.Values.OrderByDescending(c => FillScore(content, cap, c)))
            TryAdd(c);

        return new Team(TeamName(cap), cap, roster);
    }

    static int Tools(Character c) => c.Stats.Pitch + c.Stats.Bat + c.Stats.Field + c.Stats.Run;

    static int FillScore(ContentCatalog content, Character cap, Character c)
    {
        var rel = content.Chemistry.Between(cap, c);
        var chem = rel == Chemistry.Good ? 80 : rel == Chemistry.Bad ? -40 : 10;
        var same = c.Faction.Equals(cap.Faction, StringComparison.OrdinalIgnoreCase) ? 30 : 0;
        return chem + same + Tools(c);
    }
}
